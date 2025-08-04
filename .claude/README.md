# Claude Git Flow System

Dieses Verzeichnis enthält die Konfiguration und Scripts für das automatisierte Git Flow System des KGV Migration Projekts.

## 📁 Struktur

```
.claude/
├── README.md                 # Diese Datei
├── git-flow.yml             # Hauptkonfiguration für Git Flow
├── hooks/
│   └── pre-commit           # Git Pre-Commit Hook für lokale Validierung
└── logs/                    # Log-Dateien (wird automatisch erstellt)
    └── git-flow.log
```

## 🚀 Installation und Setup

### 1. Umgebungsvariablen setzen

Kopiere `.env.example` zu `.env.local` und fülle die notwendigen Werte aus:

```bash
cp .env.example .env.local
```

**Wichtige Konfigurationen:**
- `GITHUB_TOKEN`: GitHub Personal Access Token für API-Zugriff
- `CLAUDE_GIT_FLOW_ENABLED=true`: Aktiviert Git Flow Features
- `CLAUDE_DEFAULT_REVIEWER`: Standard-Reviewer für PRs

### 2. GitHub Personal Access Token erstellen

1. Gehe zu [GitHub Settings > Developer settings > Personal access tokens](https://github.com/settings/tokens)
2. Klicke auf "Generate new token (classic)"
3. Gib folgende Scopes an:
   - `repo` (Full control of private repositories)
   - `read:user` (Read user profile data)
   - `read:org` (Read org and team membership)
4. Kopiere den Token in deine `.env.local` als `GITHUB_TOKEN`

### 3. Git Hooks installieren (Optional)

Für lokale Pre-Commit Validierung:

```bash
# Symlink erstellen (empfohlen)
ln -sf ../../.claude/hooks/pre-commit .git/hooks/pre-commit

# Oder Datei kopieren
cp .claude/hooks/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

### 4. Git Flow Konfiguration validieren

```bash
# Prüfe ob alles korrekt konfiguriert ist
claude flow check
```

## 🔧 Konfiguration

### Git Flow Einstellungen (`git-flow.yml`)

Die Hauptkonfiguration befindet sich in `.claude/git-flow.yml`. Hier können folgende Aspekte angepasst werden:

- **Branch-Naming Conventions**
- **GitHub Integration Settings**
- **Commit Message Templates**
- **Automation Rules**
- **Project-spezifische Einstellungen**

### Erweiterte Konfiguration

```yaml
# Beispiel: Custom Branch-Prefixes
git_flow:
  feature_prefix: "feat/"     # statt "feature/"
  bugfix_prefix: "fix/"       # statt "bugfix/"
  
# Issue-Kategorien für automatische Zuordnung
issue_categories:
  database:
    branch_prefix: "db/"
    labels: ["database", "migration"]
    reviewers: ["@db-team"]
```

## 🌊 Git Flow Workflows

### Feature Development

```bash
# 1. Neues Feature aus GitHub Issue starten
claude issue start 123
# Erstellt: feature/ISSUE-123-user-authentication

# 2. Entwicklung und Commits
git add .
git commit -m "feat(auth): implement JWT authentication

- Add authentication middleware
- Implement user login/logout
- Add JWT token validation

Closes #123"

# 3. Feature abschließen (PR erstellen)
claude issue finish
# → Erstellt automatisch Pull Request
# → Verknüpft mit Issue #123
# → Weist Reviewer zu
```

### Release Management

```bash
# Release vorbereiten
claude flow release start 1.2.0
# → Erstellt release/v1.2.0 branch
# → Aktualisiert Versionsnummern
# → Generiert Changelog

# Release abschließen
claude flow release finish 1.2.0
# → Merged in main und develop
# → Erstellt Git Tag v1.2.0
# → Löscht Release Branch
```

### Hotfix Workflow

```bash
# Kritischer Hotfix
claude flow hotfix start 1.1.1 "security-vulnerability"
# → Erstellt hotfix/v1.1.1-security-vulnerability
# → Basiert auf main branch
# → Erstellt Tracking Issue

# Hotfix abschließen
claude flow hotfix finish 1.1.1
# → Merged in main und develop
# → Erstellt Tag v1.1.1
# → Benachrichtigt Team
```

## 🤖 Automatisierung Features

### Branch Name Intelligence

Das System konvertiert automatisch Issue-Titel zu korrekten Branch-Namen:

```
Issue #123: "Implement user authentication with Azure AD"
→ Branch: feature/ISSUE-123-user-authentication-azure-ad

Issue #124: "Fix: Docker Compose PostgreSQL connection fails"
→ Branch: bugfix/ISSUE-124-docker-compose-postgresql
```

### GitHub Integration

- **Automatische Branch-Erstellung** auf GitHub
- **Issue-Verknüpfung** und Status-Updates
- **PR-Templates** werden automatisch ausgefüllt
- **Reviewer-Zuweisung** basierend auf Konfiguration
- **Labels** werden automatisch gesetzt

### CI/CD Integration

Die GitHub Actions (`.github/workflows/git-flow-automation.yml`) führen automatisch aus:

- **Branch-Validierung**
- **Issue Status Updates**
- **Automated Testing** für Feature Branches
- **Security Scanning** für Container
- **Release Preparation**
- **Branch Cleanup** nach Merge

## 🔍 Monitoring und Status

### Branch Status anzeigen

```bash
claude status

# Output:
# 🌿 Git Flow Status
# Current Branch: feature/ISSUE-123-user-authentication
# Base Branch: develop (✅ up to date)
# GitHub Issue: #123 - "Implement user authentication" (🔄 In Progress)
# Commits ahead: 3
# Uncommitted changes: 2 files
```

### Health Check

```bash
claude flow check

# Output:
# ✅ Git Flow Configuration: Valid
# ✅ GitHub Authentication: Connected
# ✅ Branch Protection Rules: Respected
# ✅ Required Status Checks: Configured
# ⚠️  Warning: 3 stale feature branches found
```

## 🛠️ Fehlerbehebung

### Häufige Probleme

#### 1. GitHub Token Fehler
```bash
# Prüfe Token-Gültigkeit
curl -H "Authorization: token $GITHUB_TOKEN" https://api.github.com/user

# Token neu generieren falls abgelaufen
```

#### 2. Branch-Naming Fehler
```bash
# Aktueller Branch entspricht nicht den Conventions
git branch -m feature/ISSUE-123-correct-format
git push origin :old-branch-name
git push --set-upstream origin feature/ISSUE-123-correct-format
```

#### 3. Merge Konflikte
```bash
# Automatische Conflict Resolution
claude branch resolve-conflicts
# → Interaktive Lösung von Merge-Konflikten
```

#### 4. Offline-Modus
```bash
# Lokale Branches erstellen wenn GitHub nicht erreichbar
claude issue start 125 --offline
# → Erstellt lokale Branch
# → Sync mit GitHub später: claude sync
```

### Debug-Modus

```bash
# Detaillierte Logs aktivieren
export CLAUDE_DEBUG=true
claude issue start 123

# Log-Datei prüfen
tail -f .claude/logs/git-flow.log
```

## 📋 Checkliste für Setup

- [ ] `.env.local` erstellt und konfiguriert
- [ ] `GITHUB_TOKEN` gesetzt und validiert
- [ ] Git Hooks installiert (optional)
- [ ] `claude flow check` erfolgreich
- [ ] Test-Feature Branch erstellt
- [ ] Team über neue Workflows informiert

## 🔗 Weiterführende Links

- [GitHub Personal Access Tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Git Flow Workflow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)

## 📞 Support

Bei Fragen oder Problemen:

1. Prüfe die Logs: `.claude/logs/git-flow.log`
2. Validiere die Konfiguration: `claude flow check`
3. Erstelle ein Issue mit dem Label `git-flow-support`

---

**Version**: 1.0.0  
**Letzte Aktualisierung**: 2025-08-04  
**Autor**: KGV Migration Team