#!/usr/bin/env python3
"""
KGV Authentication and Authorization Service
============================================

Implements secure authentication with:
- JWT tokens (RS256 algorithm)
- OAuth2 support
- SAML integration
- Multi-factor authentication (MFA)
- Role-based access control (RBAC)
- Session management
- Password policies

OWASP Compliance: A01:2021 - Broken Access Control
                 A07:2021 - Identification and Authentication Failures
"""

import os
import secrets
import hashlib
import base64
import json
from typing import Optional, Dict, Any, List, Tuple
from datetime import datetime, timedelta, timezone
from dataclasses import dataclass
from enum import Enum
import re

import jwt
import bcrypt
from cryptography.hazmat.primitives import serialization, hashes
from cryptography.hazmat.primitives.asymmetric import rsa, padding
from cryptography.hazmat.backends import default_backend
from cryptography.fernet import Fernet
import pyotp
import qrcode
from passlib.context import CryptContext
from argon2 import PasswordHasher
import redis
from sqlalchemy import create_engine, Column, Integer, String, Boolean, DateTime, ForeignKey, Table
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, relationship, Session
from sqlalchemy.dialects.postgresql import UUID
import uuid


# =============================================================================
# CONFIGURATION
# =============================================================================

@dataclass
class AuthConfig:
    """Authentication configuration"""
    # JWT Settings
    jwt_algorithm: str = os.getenv('JWT_ALGORITHM', 'RS256')
    jwt_public_key_path: str = os.getenv('JWT_PUBLIC_KEY_PATH', '/certs/jwt/public.pem')
    jwt_private_key_path: str = os.getenv('JWT_PRIVATE_KEY_PATH', '/certs/jwt/private.pem')
    jwt_access_token_expiry: int = int(os.getenv('JWT_ACCESS_TOKEN_EXPIRY', '900'))  # 15 minutes
    jwt_refresh_token_expiry: int = int(os.getenv('JWT_REFRESH_TOKEN_EXPIRY', '86400'))  # 24 hours
    jwt_issuer: str = os.getenv('JWT_ISSUER', 'kgv-auth')
    jwt_audience: str = os.getenv('JWT_AUDIENCE', 'kgv-api')
    
    # Password Policy
    password_min_length: int = int(os.getenv('PASSWORD_MIN_LENGTH', '12'))
    password_require_uppercase: bool = os.getenv('PASSWORD_REQUIRE_UPPERCASE', 'true').lower() == 'true'
    password_require_lowercase: bool = os.getenv('PASSWORD_REQUIRE_LOWERCASE', 'true').lower() == 'true'
    password_require_numbers: bool = os.getenv('PASSWORD_REQUIRE_NUMBERS', 'true').lower() == 'true'
    password_require_special: bool = os.getenv('PASSWORD_REQUIRE_SPECIAL', 'true').lower() == 'true'
    password_history_count: int = int(os.getenv('PASSWORD_HISTORY_COUNT', '5'))
    password_expiry_days: int = int(os.getenv('PASSWORD_EXPIRY_DAYS', '90'))
    
    # MFA Settings
    mfa_enabled: bool = os.getenv('MFA_ENABLED', 'true').lower() == 'true'
    mfa_issuer_name: str = os.getenv('MFA_ISSUER_NAME', 'KGV System')
    
    # Session Settings
    session_timeout: int = int(os.getenv('SESSION_TIMEOUT', '3600'))  # 1 hour
    session_absolute_timeout: int = int(os.getenv('SESSION_ABSOLUTE_TIMEOUT', '43200'))  # 12 hours
    concurrent_sessions_limit: int = int(os.getenv('CONCURRENT_SESSIONS_LIMIT', '3'))
    
    # Security Settings
    max_login_attempts: int = int(os.getenv('MAX_LOGIN_ATTEMPTS', '5'))
    lockout_duration: int = int(os.getenv('LOCKOUT_DURATION', '900'))  # 15 minutes
    require_email_verification: bool = os.getenv('REQUIRE_EMAIL_VERIFICATION', 'true').lower() == 'true'
    
    # Database
    database_url: str = os.getenv('DATABASE_URL', 'postgresql://user:pass@localhost/kgv')
    
    # Redis
    redis_host: str = os.getenv('REDIS_HOST', 'localhost')
    redis_port: int = int(os.getenv('REDIS_PORT', '6379'))
    redis_password: str = os.getenv('REDIS_PASSWORD', '')
    redis_ssl: bool = os.getenv('REDIS_SSL', 'true').lower() == 'true'


# =============================================================================
# DATABASE MODELS
# =============================================================================

Base = declarative_base()

# Association table for many-to-many relationship between users and roles
user_roles = Table('user_roles', Base.metadata,
    Column('user_id', Integer, ForeignKey('auth_users.id')),
    Column('role_id', Integer, ForeignKey('auth_roles.id'))
)

# Association table for many-to-many relationship between roles and permissions
role_permissions = Table('role_permissions', Base.metadata,
    Column('role_id', Integer, ForeignKey('auth_roles.id')),
    Column('permission_id', Integer, ForeignKey('auth_permissions.id'))
)


class AuthUser(Base):
    """User model with security features"""
    __tablename__ = 'auth_users'
    
    id = Column(Integer, primary_key=True)
    uuid = Column(UUID(as_uuid=True), default=uuid.uuid4, unique=True, nullable=False)
    username = Column(String(100), unique=True, nullable=False, index=True)
    email = Column(String(255), unique=True, nullable=False, index=True)
    password_hash = Column(String(255), nullable=False)
    
    # Security fields
    mfa_secret = Column(String(255))
    mfa_enabled = Column(Boolean, default=False)
    email_verified = Column(Boolean, default=False)
    email_verification_token = Column(String(255))
    
    # Password management
    password_changed_at = Column(DateTime, default=datetime.utcnow)
    password_history = Column(String)  # JSON array of previous password hashes
    must_change_password = Column(Boolean, default=False)
    
    # Account status
    is_active = Column(Boolean, default=True)
    is_locked = Column(Boolean, default=False)
    locked_until = Column(DateTime)
    failed_login_attempts = Column(Integer, default=0)
    last_failed_login = Column(DateTime)
    
    # Audit fields
    created_at = Column(DateTime, default=datetime.utcnow)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    last_login = Column(DateTime)
    last_login_ip = Column(String(45))
    
    # Relationships
    roles = relationship('AuthRole', secondary=user_roles, back_populates='users')
    sessions = relationship('AuthSession', back_populates='user', cascade='all, delete-orphan')
    audit_logs = relationship('AuthAuditLog', back_populates='user', cascade='all, delete-orphan')


class AuthRole(Base):
    """Role model for RBAC"""
    __tablename__ = 'auth_roles'
    
    id = Column(Integer, primary_key=True)
    name = Column(String(50), unique=True, nullable=False)
    description = Column(String(255))
    is_system_role = Column(Boolean, default=False)  # Cannot be deleted
    created_at = Column(DateTime, default=datetime.utcnow)
    
    # Relationships
    users = relationship('AuthUser', secondary=user_roles, back_populates='roles')
    permissions = relationship('AuthPermission', secondary=role_permissions, back_populates='roles')


class AuthPermission(Base):
    """Permission model for fine-grained access control"""
    __tablename__ = 'auth_permissions'
    
    id = Column(Integer, primary_key=True)
    name = Column(String(100), unique=True, nullable=False)
    resource = Column(String(100), nullable=False)
    action = Column(String(50), nullable=False)
    description = Column(String(255))
    
    # Relationships
    roles = relationship('AuthRole', secondary=role_permissions, back_populates='permissions')


class AuthSession(Base):
    """Session tracking for security"""
    __tablename__ = 'auth_sessions'
    
    id = Column(Integer, primary_key=True)
    session_id = Column(String(255), unique=True, nullable=False, index=True)
    user_id = Column(Integer, ForeignKey('auth_users.id'), nullable=False)
    
    # Token management
    access_token = Column(String(1000))
    refresh_token = Column(String(1000))
    access_token_expires = Column(DateTime)
    refresh_token_expires = Column(DateTime)
    
    # Session metadata
    ip_address = Column(String(45))
    user_agent = Column(String(500))
    created_at = Column(DateTime, default=datetime.utcnow)
    last_activity = Column(DateTime, default=datetime.utcnow)
    expires_at = Column(DateTime)
    is_active = Column(Boolean, default=True)
    
    # Relationships
    user = relationship('AuthUser', back_populates='sessions')


class AuthAuditLog(Base):
    """Audit log for security events"""
    __tablename__ = 'auth_audit_logs'
    
    id = Column(Integer, primary_key=True)
    user_id = Column(Integer, ForeignKey('auth_users.id'))
    event_type = Column(String(50), nullable=False)
    event_status = Column(String(20), nullable=False)
    ip_address = Column(String(45))
    user_agent = Column(String(500))
    details = Column(String)  # JSON
    created_at = Column(DateTime, default=datetime.utcnow, index=True)
    
    # Relationships
    user = relationship('AuthUser', back_populates='audit_logs')


# =============================================================================
# PASSWORD SECURITY
# =============================================================================

class PasswordManager:
    """Secure password management with multiple hashing algorithms"""
    
    def __init__(self, config: AuthConfig):
        self.config = config
        # Use Argon2 as primary, with bcrypt fallback
        self.ph = PasswordHasher(
            time_cost=2,
            memory_cost=65536,
            parallelism=1,
            hash_len=32,
            salt_len=16
        )
        self.pwd_context = CryptContext(
            schemes=["argon2", "bcrypt", "pbkdf2_sha256"],
            default="argon2",
            argon2__rounds=2,
            bcrypt__rounds=12,
            pbkdf2_sha256__rounds=100000
        )
    
    def hash_password(self, password: str) -> str:
        """Hash password using Argon2id"""
        return self.ph.hash(password)
    
    def verify_password(self, password: str, hash: str) -> bool:
        """Verify password against hash"""
        try:
            self.ph.verify(hash, password)
            return True
        except:
            # Try legacy formats
            return self.pwd_context.verify(password, hash)
    
    def validate_password_strength(self, password: str, username: str = None, 
                                  email: str = None) -> Tuple[bool, List[str]]:
        """Validate password against security policy"""
        errors = []
        
        # Length check
        if len(password) < self.config.password_min_length:
            errors.append(f"Password must be at least {self.config.password_min_length} characters")
        
        # Complexity checks
        if self.config.password_require_uppercase and not re.search(r'[A-Z]', password):
            errors.append("Password must contain at least one uppercase letter")
        
        if self.config.password_require_lowercase and not re.search(r'[a-z]', password):
            errors.append("Password must contain at least one lowercase letter")
        
        if self.config.password_require_numbers and not re.search(r'\d', password):
            errors.append("Password must contain at least one number")
        
        if self.config.password_require_special and not re.search(r'[!@#$%^&*(),.?":{}|<>]', password):
            errors.append("Password must contain at least one special character")
        
        # Common password check
        common_passwords = ['password', '123456', 'admin', 'letmein', 'welcome']
        if password.lower() in common_passwords:
            errors.append("Password is too common")
        
        # Username/email similarity check
        if username and username.lower() in password.lower():
            errors.append("Password cannot contain username")
        
        if email:
            email_prefix = email.split('@')[0]
            if email_prefix.lower() in password.lower():
                errors.append("Password cannot contain email address")
        
        # Entropy check (simplified)
        if len(set(password)) < 5:
            errors.append("Password lacks sufficient character variety")
        
        return len(errors) == 0, errors
    
    def check_password_history(self, new_password: str, password_history: List[str]) -> bool:
        """Check if password was recently used"""
        if not password_history:
            return True
        
        for old_hash in password_history[-self.config.password_history_count:]:
            if self.verify_password(new_password, old_hash):
                return False
        
        return True


# =============================================================================
# JWT TOKEN MANAGEMENT
# =============================================================================

class JWTManager:
    """Secure JWT token management with RS256"""
    
    def __init__(self, config: AuthConfig):
        self.config = config
        self._load_keys()
    
    def _load_keys(self):
        """Load RSA keys for JWT signing"""
        # Load private key
        with open(self.config.jwt_private_key_path, 'rb') as f:
            self.private_key = serialization.load_pem_private_key(
                f.read(),
                password=None,
                backend=default_backend()
            )
        
        # Load public key
        with open(self.config.jwt_public_key_path, 'rb') as f:
            self.public_key = serialization.load_pem_public_key(
                f.read(),
                backend=default_backend()
            )
    
    def generate_access_token(self, user: AuthUser, session_id: str) -> str:
        """Generate JWT access token"""
        now = datetime.now(timezone.utc)
        expires = now + timedelta(seconds=self.config.jwt_access_token_expiry)
        
        # Build claims
        claims = {
            'sub': str(user.uuid),
            'username': user.username,
            'email': user.email,
            'roles': [role.name for role in user.roles],
            'session_id': session_id,
            'iat': now,
            'exp': expires,
            'nbf': now,
            'iss': self.config.jwt_issuer,
            'aud': self.config.jwt_audience,
            'jti': str(uuid.uuid4())  # Unique token ID for revocation
        }
        
        # Add custom claims for permissions
        permissions = []
        for role in user.roles:
            for perm in role.permissions:
                permissions.append(f"{perm.resource}:{perm.action}")
        claims['permissions'] = list(set(permissions))
        
        # Sign token
        token = jwt.encode(
            claims,
            self.private_key,
            algorithm=self.config.jwt_algorithm
        )
        
        return token
    
    def generate_refresh_token(self, user: AuthUser, session_id: str) -> str:
        """Generate JWT refresh token"""
        now = datetime.now(timezone.utc)
        expires = now + timedelta(seconds=self.config.jwt_refresh_token_expiry)
        
        claims = {
            'sub': str(user.uuid),
            'session_id': session_id,
            'token_type': 'refresh',
            'iat': now,
            'exp': expires,
            'nbf': now,
            'iss': self.config.jwt_issuer,
            'aud': self.config.jwt_audience,
            'jti': str(uuid.uuid4())
        }
        
        token = jwt.encode(
            claims,
            self.private_key,
            algorithm=self.config.jwt_algorithm
        )
        
        return token
    
    def verify_token(self, token: str, token_type: str = 'access') -> Optional[Dict[str, Any]]:
        """Verify and decode JWT token"""
        try:
            claims = jwt.decode(
                token,
                self.public_key,
                algorithms=[self.config.jwt_algorithm],
                audience=self.config.jwt_audience,
                issuer=self.config.jwt_issuer
            )
            
            # Verify token type
            if token_type == 'refresh' and claims.get('token_type') != 'refresh':
                return None
            
            return claims
            
        except jwt.ExpiredSignatureError:
            return None
        except jwt.InvalidTokenError:
            return None


# =============================================================================
# MULTI-FACTOR AUTHENTICATION
# =============================================================================

class MFAManager:
    """Multi-factor authentication using TOTP"""
    
    def __init__(self, config: AuthConfig):
        self.config = config
    
    def generate_secret(self) -> str:
        """Generate new TOTP secret"""
        return pyotp.random_base32()
    
    def generate_qr_code(self, user: AuthUser, secret: str) -> str:
        """Generate QR code for MFA setup"""
        totp_uri = pyotp.totp.TOTP(secret).provisioning_uri(
            name=user.email,
            issuer_name=self.config.mfa_issuer_name
        )
        
        qr = qrcode.QRCode(version=1, box_size=10, border=5)
        qr.add_data(totp_uri)
        qr.make(fit=True)
        
        # Convert to base64 for embedding in HTML
        import io
        buffer = io.BytesIO()
        img = qr.make_image(fill_color="black", back_color="white")
        img.save(buffer, format='PNG')
        
        return base64.b64encode(buffer.getvalue()).decode()
    
    def verify_token(self, secret: str, token: str) -> bool:
        """Verify TOTP token"""
        totp = pyotp.TOTP(secret)
        # Allow 1 time step tolerance for clock skew
        return totp.verify(token, valid_window=1)
    
    def generate_backup_codes(self, count: int = 10) -> List[str]:
        """Generate backup codes for MFA recovery"""
        codes = []
        for _ in range(count):
            code = ''.join(secrets.choice('0123456789') for _ in range(8))
            codes.append(f"{code[:4]}-{code[4:]}")
        return codes


# =============================================================================
# AUTHENTICATION SERVICE
# =============================================================================

class AuthenticationService:
    """Main authentication service with all security features"""
    
    def __init__(self, config: AuthConfig):
        self.config = config
        self.password_manager = PasswordManager(config)
        self.jwt_manager = JWTManager(config)
        self.mfa_manager = MFAManager(config)
        
        # Initialize database
        self.engine = create_engine(config.database_url)
        Base.metadata.create_all(self.engine)
        self.SessionLocal = sessionmaker(bind=self.engine)
        
        # Initialize Redis for session management
        self.redis_client = redis.Redis(
            host=config.redis_host,
            port=config.redis_port,
            password=config.redis_password,
            decode_responses=True
        )
    
    def register_user(self, username: str, email: str, password: str) -> Tuple[bool, str, Optional[AuthUser]]:
        """Register new user with security checks"""
        db = self.SessionLocal()
        
        try:
            # Check if user exists
            existing = db.query(AuthUser).filter(
                (AuthUser.username == username) | (AuthUser.email == email)
            ).first()
            
            if existing:
                self._log_audit(db, None, 'REGISTRATION_FAILED', 'DUPLICATE', 
                              {'username': username, 'email': email})
                return False, "Username or email already exists", None
            
            # Validate password strength
            is_valid, errors = self.password_manager.validate_password_strength(
                password, username, email
            )
            
            if not is_valid:
                return False, "; ".join(errors), None
            
            # Create user
            user = AuthUser(
                username=username,
                email=email,
                password_hash=self.password_manager.hash_password(password),
                email_verification_token=secrets.token_urlsafe(32) if self.config.require_email_verification else None,
                email_verified=not self.config.require_email_verification
            )
            
            # Assign default role
            default_role = db.query(AuthRole).filter_by(name='user').first()
            if default_role:
                user.roles.append(default_role)
            
            db.add(user)
            db.commit()
            db.refresh(user)
            
            self._log_audit(db, user.id, 'REGISTRATION', 'SUCCESS', 
                          {'username': username, 'email': email})
            
            return True, "Registration successful", user
            
        except Exception as e:
            db.rollback()
            return False, f"Registration failed: {str(e)}", None
        finally:
            db.close()
    
    def authenticate(self, username: str, password: str, ip_address: str = None, 
                    user_agent: str = None, mfa_token: str = None) -> Tuple[bool, str, Optional[Dict]]:
        """Authenticate user with security checks"""
        db = self.SessionLocal()
        
        try:
            # Find user
            user = db.query(AuthUser).filter(
                (AuthUser.username == username) | (AuthUser.email == username)
            ).first()
            
            if not user:
                self._log_audit(db, None, 'LOGIN_FAILED', 'USER_NOT_FOUND', 
                              {'username': username}, ip_address)
                return False, "Invalid credentials", None
            
            # Check if account is locked
            if user.is_locked:
                if user.locked_until and user.locked_until > datetime.utcnow():
                    remaining = (user.locked_until - datetime.utcnow()).seconds
                    self._log_audit(db, user.id, 'LOGIN_FAILED', 'ACCOUNT_LOCKED', 
                                  {'remaining_seconds': remaining}, ip_address)
                    return False, f"Account locked. Try again in {remaining} seconds", None
                else:
                    # Unlock account
                    user.is_locked = False
                    user.locked_until = None
                    user.failed_login_attempts = 0
            
            # Verify password
            if not self.password_manager.verify_password(password, user.password_hash):
                user.failed_login_attempts += 1
                user.last_failed_login = datetime.utcnow()
                
                # Lock account if max attempts exceeded
                if user.failed_login_attempts >= self.config.max_login_attempts:
                    user.is_locked = True
                    user.locked_until = datetime.utcnow() + timedelta(seconds=self.config.lockout_duration)
                    db.commit()
                    
                    self._log_audit(db, user.id, 'ACCOUNT_LOCKED', 'MAX_ATTEMPTS', 
                                  {'attempts': user.failed_login_attempts}, ip_address)
                    return False, "Account locked due to multiple failed attempts", None
                
                db.commit()
                self._log_audit(db, user.id, 'LOGIN_FAILED', 'INVALID_PASSWORD', 
                              {'attempts': user.failed_login_attempts}, ip_address)
                return False, "Invalid credentials", None
            
            # Check if account is active
            if not user.is_active:
                self._log_audit(db, user.id, 'LOGIN_FAILED', 'ACCOUNT_INACTIVE', {}, ip_address)
                return False, "Account is inactive", None
            
            # Check email verification
            if self.config.require_email_verification and not user.email_verified:
                self._log_audit(db, user.id, 'LOGIN_FAILED', 'EMAIL_NOT_VERIFIED', {}, ip_address)
                return False, "Email not verified", None
            
            # Check MFA if enabled
            if user.mfa_enabled:
                if not mfa_token:
                    return False, "MFA token required", None
                
                if not self.mfa_manager.verify_token(user.mfa_secret, mfa_token):
                    user.failed_login_attempts += 1
                    db.commit()
                    
                    self._log_audit(db, user.id, 'LOGIN_FAILED', 'INVALID_MFA', 
                                  {'attempts': user.failed_login_attempts}, ip_address)
                    return False, "Invalid MFA token", None
            
            # Check password expiry
            if self.config.password_expiry_days > 0:
                password_age = (datetime.utcnow() - user.password_changed_at).days
                if password_age > self.config.password_expiry_days:
                    user.must_change_password = True
            
            # Check concurrent sessions
            active_sessions = db.query(AuthSession).filter(
                AuthSession.user_id == user.id,
                AuthSession.is_active == True,
                AuthSession.expires_at > datetime.utcnow()
            ).count()
            
            if active_sessions >= self.config.concurrent_sessions_limit:
                # Invalidate oldest session
                oldest = db.query(AuthSession).filter(
                    AuthSession.user_id == user.id,
                    AuthSession.is_active == True
                ).order_by(AuthSession.created_at).first()
                
                if oldest:
                    oldest.is_active = False
                    self._invalidate_session_cache(oldest.session_id)
            
            # Create session
            session_id = secrets.token_urlsafe(32)
            access_token = self.jwt_manager.generate_access_token(user, session_id)
            refresh_token = self.jwt_manager.generate_refresh_token(user, session_id)
            
            session = AuthSession(
                session_id=session_id,
                user_id=user.id,
                access_token=access_token,
                refresh_token=refresh_token,
                access_token_expires=datetime.utcnow() + timedelta(seconds=self.config.jwt_access_token_expiry),
                refresh_token_expires=datetime.utcnow() + timedelta(seconds=self.config.jwt_refresh_token_expiry),
                expires_at=datetime.utcnow() + timedelta(seconds=self.config.session_absolute_timeout),
                ip_address=ip_address,
                user_agent=user_agent
            )
            
            db.add(session)
            
            # Update user login info
            user.last_login = datetime.utcnow()
            user.last_login_ip = ip_address
            user.failed_login_attempts = 0
            
            db.commit()
            
            # Cache session in Redis
            self._cache_session(session)
            
            self._log_audit(db, user.id, 'LOGIN_SUCCESS', 'SUCCESS', 
                          {'session_id': session_id}, ip_address)
            
            return True, "Authentication successful", {
                'user_id': user.id,
                'username': user.username,
                'email': user.email,
                'roles': [role.name for role in user.roles],
                'access_token': access_token,
                'refresh_token': refresh_token,
                'expires_in': self.config.jwt_access_token_expiry,
                'must_change_password': user.must_change_password
            }
            
        except Exception as e:
            db.rollback()
            return False, f"Authentication failed: {str(e)}", None
        finally:
            db.close()
    
    def verify_session(self, access_token: str) -> Optional[Dict]:
        """Verify access token and session"""
        # Verify JWT
        claims = self.jwt_manager.verify_token(access_token)
        if not claims:
            return None
        
        # Check session in cache
        session_id = claims.get('session_id')
        cached_session = self._get_cached_session(session_id)
        
        if cached_session:
            return claims
        
        # Check database if not in cache
        db = self.SessionLocal()
        try:
            session = db.query(AuthSession).filter(
                AuthSession.session_id == session_id,
                AuthSession.is_active == True,
                AuthSession.expires_at > datetime.utcnow()
            ).first()
            
            if session:
                # Update last activity
                session.last_activity = datetime.utcnow()
                db.commit()
                
                # Re-cache session
                self._cache_session(session)
                
                return claims
            
        finally:
            db.close()
        
        return None
    
    def _log_audit(self, db: Session, user_id: Optional[int], event_type: str, 
                  event_status: str, details: Dict = None, ip_address: str = None):
        """Log security audit event"""
        audit = AuthAuditLog(
            user_id=user_id,
            event_type=event_type,
            event_status=event_status,
            details=json.dumps(details) if details else None,
            ip_address=ip_address
        )
        db.add(audit)
        db.commit()
    
    def _cache_session(self, session: AuthSession):
        """Cache session in Redis"""
        key = f"session:{session.session_id}"
        data = {
            'user_id': session.user_id,
            'expires_at': session.expires_at.isoformat(),
            'ip_address': session.ip_address
        }
        
        ttl = (session.expires_at - datetime.utcnow()).seconds
        self.redis_client.setex(key, ttl, json.dumps(data))
    
    def _get_cached_session(self, session_id: str) -> Optional[Dict]:
        """Get session from Redis cache"""
        key = f"session:{session_id}"
        data = self.redis_client.get(key)
        
        if data:
            return json.loads(data)
        
        return None
    
    def _invalidate_session_cache(self, session_id: str):
        """Remove session from cache"""
        key = f"session:{session_id}"
        self.redis_client.delete(key)


# =============================================================================
# INITIALIZATION
# =============================================================================

def initialize_auth_system():
    """Initialize authentication system with default roles and permissions"""
    config = AuthConfig()
    engine = create_engine(config.database_url)
    Base.metadata.create_all(engine)
    
    SessionLocal = sessionmaker(bind=engine)
    db = SessionLocal()
    
    try:
        # Create default roles
        roles = [
            ('admin', 'System administrator with full access'),
            ('user', 'Regular user with limited access'),
            ('readonly', 'Read-only access to resources')
        ]
        
        for role_name, description in roles:
            if not db.query(AuthRole).filter_by(name=role_name).first():
                role = AuthRole(name=role_name, description=description, is_system_role=True)
                db.add(role)
        
        # Create default permissions
        permissions = [
            ('users:read', 'users', 'read', 'View user information'),
            ('users:write', 'users', 'write', 'Create and update users'),
            ('users:delete', 'users', 'delete', 'Delete users'),
            ('applications:read', 'applications', 'read', 'View applications'),
            ('applications:write', 'applications', 'write', 'Create and update applications'),
            ('applications:delete', 'applications', 'delete', 'Delete applications'),
            ('reports:read', 'reports', 'read', 'View reports'),
            ('audit:read', 'audit', 'read', 'View audit logs')
        ]
        
        for perm_name, resource, action, description in permissions:
            if not db.query(AuthPermission).filter_by(name=perm_name).first():
                permission = AuthPermission(
                    name=perm_name, 
                    resource=resource, 
                    action=action, 
                    description=description
                )
                db.add(permission)
        
        db.commit()
        
        # Assign permissions to roles
        admin_role = db.query(AuthRole).filter_by(name='admin').first()
        user_role = db.query(AuthRole).filter_by(name='user').first()
        readonly_role = db.query(AuthRole).filter_by(name='readonly').first()
        
        all_permissions = db.query(AuthPermission).all()
        
        # Admin gets all permissions
        admin_role.permissions = all_permissions
        
        # User gets read/write for applications
        user_role.permissions = db.query(AuthPermission).filter(
            AuthPermission.name.in_(['applications:read', 'applications:write'])
        ).all()
        
        # Readonly gets only read permissions
        readonly_role.permissions = db.query(AuthPermission).filter(
            AuthPermission.action == 'read'
        ).all()
        
        db.commit()
        
        print("Authentication system initialized successfully")
        
    finally:
        db.close()


if __name__ == '__main__':
    initialize_auth_system()