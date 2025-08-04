# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a comprehensive KGV (Kleingartenverein - German allotment garden association) migration project. Originally a database migration project, it has evolved into a full-stack modernization effort including:

- **Legacy System Migration**: From Visual Basic desktop application to modern web application
- **Database Modernization**: SQL Server 2004 → PostgreSQL 16 with container-native deployment
- **Technology Stack**: .NET 9 Web API + Next.js Frontend with Tailwind CSS
- **Infrastructure**: Cloud-agnostic container architecture (Docker Compose + Kubernetes)
- **Architecture Patterns**: Clean Architecture with Azure-inspired resilience patterns

## Database Schema

The project centers around a SQL Server database with 10 main entities:

- **Aktenzeichen** - File reference numbers with district, number, and year
- **Antrag** - Applications with personal data, contact info, and application status
- **Bezirk** - Districts/regions
- **Bezirke_Katasterbezirke** - Junction table linking districts to cadastral areas
- **Eingangsnummer** - Entry numbers
- **Katasterbezirk** - Cadastral districts
- **Kennungen** - Identifiers/codes
- **Mischenfelder** - Mixed/miscellaneous fields
- **Personen** - Person records
- **Verlauf** - History/timeline records

## Key Files

- `old/kgv.sql` - Complete database schema export (182 lines) containing CREATE TABLE statements for all entities
- `README.md` - Basic project description
- `design-system.md` - Complete Tailwind CSS component library
- `src/KGV.Infrastructure/Patterns/` - Architecture patterns implementation
- `postgresql/migration/` - Database modernization scripts
- `infrastructure/docker/` - Container configurations

## Development Environment

### Quick Start
```bash
# Clone and start complete development environment
git clone <repo>
cd kgv_migration
cp .env.example .env.local
docker-compose up -d

# Services will be available at:
# http://localhost:3000  - Next.js Frontend
# http://localhost:5000  - .NET 9 Web API
# http://localhost:5432  - PostgreSQL Database
# http://localhost:6379  - Redis Cache
```

### Testing Commands
```bash
# Run all tests
docker-compose exec kgv-api dotnet test
docker-compose exec kgv-frontend npm test

# Run linting
docker-compose exec kgv-api dotnet format --verify-no-changes
docker-compose exec kgv-frontend npm run lint

# Database migrations
docker-compose exec postgres psql -U kgv_user -d KGV -f /docker-entrypoint-initdb.d/01_schema_core.sql
```

---

# 🔄 Git Flow & Branch Automation System

## Git Flow Configuration

This repository uses an automated Git Flow workflow with GitHub integration for streamlined development.

### Branch Structure
```
main (production-ready code)
├── develop (integration branch)
├── feature/ISSUE-123-user-authentication
├── feature/ISSUE-124-docker-compose-setup
├── release/v1.2.0
└── hotfix/v1.1.1-critical-security-fix
```

### Branch Naming Conventions
- **Features**: `feature/ISSUE-{number}-{description}` (e.g., `feature/ISSUE-123-user-authentication`)
- **Releases**: `release/v{major}.{minor}.{patch}` (e.g., `release/v1.2.0`)
- **Hotfixes**: `hotfix/v{major}.{minor}.{patch}-{description}` (e.g., `hotfix/v1.1.1-security-fix`)
- **Bug Fixes**: `bugfix/ISSUE-{number}-{description}` (e.g., `bugfix/ISSUE-125-login-error`)

## Automated Issue → Branch Workflow

### Quick Commands
```bash
# Create feature branch from GitHub issue
claude issue start 123
claude issue start https://github.com/andrekirst/kgv_migration/issues/123

# Create branch for current context (if issue detected)
claude branch create

# Finish feature (create PR and cleanup)
claude issue finish

# Switch to develop and pull latest
claude flow develop

# Start release process
claude flow release start 1.2.0

# Finish release (merge to main + develop, create tag)
claude flow release finish 1.2.0

# Emergency hotfix
claude flow hotfix start 1.1.1 "critical-security-fix"
```

### GitHub Integration

#### Automatic Branch Creation
When starting work on an issue, the system will:
1. **Parse Issue**: Extract issue number and title from URL or ID
2. **Generate Branch Name**: Convert issue title to kebab-case branch name
3. **Create Remote Branch**: Use GitHub API to create branch on GitHub
4. **Create Local Branch**: Checkout new branch locally
5. **Link Issue**: Associate branch with GitHub issue via metadata

#### Pull Request Automation
When finishing an issue:
1. **Push Changes**: Ensure all commits are pushed to remote
2. **Create Pull Request**: Auto-generate PR with issue reference
3. **Fill PR Template**: Use `.github/pull_request_template.md` if available
4. **Assign Reviewers**: Based on repository settings or team configuration
5. **Link Issues**: Automatically close issue when PR is merged

## Configuration

### Repository Configuration
Create `.claude/git-flow.yml`:
```yaml
# Git Flow Configuration
git_flow:
  enabled: true
  main_branch: main
  develop_branch: develop
  
  # Branch Prefixes
  feature_prefix: "feature/"
  release_prefix: "release/"
  hotfix_prefix: "hotfix/"
  bugfix_prefix: "bugfix/"
  
  # Naming Conventions
  issue_branch_format: "{prefix}ISSUE-{number}-{description}"
  max_branch_name_length: 50
  description_max_words: 4

# GitHub Integration
github:
  enabled: true
  token_env: "GITHUB_TOKEN"
  auto_create_branch: true
  auto_link_issues: true
  auto_create_pr: true
  
  # PR Configuration
  pr_template: ".github/pull_request_template.md"
  auto_assign_reviewers: true
  default_reviewers: ["@team-leads"]
  
  # Branch Protection
  respect_branch_protection: true
  required_status_checks: true

# Commit Configuration
commits:
  conventional_commits: true
  issue_reference_required: true
  commit_message_template: "{type}({scope}): {description}\n\nCloses #{issue_number}"
  
  # Pre-commit Hooks
  pre_commit_hooks:
    - branch_name_validation
    - commit_message_format
    - issue_reference_check
    - lint_staged_files

# Workflow Automation
automation:
  auto_checkout_develop_after_finish: true
  auto_delete_merged_branches: true
  auto_pull_before_create: true
  create_draft_pr_for_wip: true
```

### Environment Setup
```bash
# Required environment variables
export GITHUB_TOKEN="ghp_your_github_personal_access_token"
export CLAUDE_GIT_FLOW_ENABLED="true"

# Optional configurations
export CLAUDE_DEFAULT_REVIEWER="@andrekirst"
export CLAUDE_AUTO_ASSIGN="true"
```

## Workflow Examples

### Feature Development Workflow
```bash
# 1. Start new feature from issue
claude issue start 126
# → Creates branch: feature/ISSUE-126-postgresql-migration-scripts
# → Switches to branch and pulls latest develop

# 2. Work on feature (make commits)
git add .
git commit -m "feat(migration): add PostgreSQL schema migration scripts

- Add 01_schema_core.sql for main entities
- Add 02_indexes_performance.sql for optimizations
- Add validation for data integrity

Closes #126"

# 3. Push and create PR
claude issue finish
# → Pushes branch to GitHub
# → Creates Pull Request with auto-generated description
# → Links to Issue #126
# → Assigns reviewers based on configuration

# 4. After PR is merged, cleanup
claude branch cleanup
# → Deletes local and remote feature branch
# → Switches back to develop
# → Pulls latest changes
```

### Release Workflow
```bash
# 1. Start release preparation
claude flow release start 1.2.0
# → Creates release/v1.2.0 branch from develop
# → Updates version numbers in configuration files

# 2. Release testing and bug fixes
# (Make final adjustments, update CHANGELOG.md)

# 3. Finish release
claude flow release finish 1.2.0
# → Merges release branch to main
# → Creates git tag v1.2.0
# → Merges back to develop
# → Deletes release branch
# → Triggers deployment pipeline (if configured)
```

### Hotfix Workflow
```bash
# 1. Emergency fix needed in production
claude flow hotfix start 1.1.1 "security-vulnerability"
# → Creates hotfix/v1.1.1-security-vulnerability from main
# → Switches to hotfix branch

# 2. Apply critical fix
git add .
git commit -m "fix(security): patch SQL injection vulnerability

- Sanitize user input in search queries
- Add parameter validation
- Update security tests

Fixes critical security issue reported in production"

# 3. Finish hotfix
claude flow hotfix finish 1.1.1
# → Merges to main and develop
# → Creates tag v1.1.1
# → Triggers emergency deployment
# → Notifies team via configured channels
```

## Branch Naming Intelligence

### Automatic Name Generation
The system intelligently converts GitHub issue titles to branch names:

```
Issue #123: "Implement user authentication with Azure AD integration"
→ Branch: feature/ISSUE-123-user-authentication-azure-ad

Issue #124: "Fix: Docker Compose PostgreSQL connection fails on M1 Macs"
→ Branch: bugfix/ISSUE-124-docker-compose-postgresql-m1

Issue #125: "Add German localization for admin interface"
→ Branch: feature/ISSUE-125-german-localization-admin
```

### Special Character Handling
- **Umlaute**: `ä→ae, ö→oe, ü→ue, ß→ss`
- **Spaces**: Converted to hyphens
- **Special Chars**: Removed or converted to alphanumeric
- **Length Limit**: Truncated to configured maximum (default: 50 chars)
- **Duplicate Prevention**: Automatic suffix if branch exists

## Status & Monitoring

### Branch Status Display
```bash
# Show current Git Flow status
claude status

# Output:
# 🌿 Git Flow Status
# Current Branch: feature/ISSUE-123-user-authentication
# Base Branch: develop (✅ up to date)
# GitHub Issue: #123 - "Implement user authentication" (🔄 In Progress)
# Commits ahead: 3
# Uncommitted changes: 2 files
# 
# 📋 Active Issues:
# #123 - In Progress (feature/ISSUE-123-user-authentication)
# #124 - Ready for Review (feature/ISSUE-124-docker-setup)
# 
# 🔄 Open Pull Requests:
# #15 - feature/ISSUE-124-docker-setup → develop (2/3 approvals)
```

### Git Flow Health Check
```bash
# Validate Git Flow setup
claude flow check

# Output:
# ✅ Git Flow Configuration: Valid
# ✅ GitHub Authentication: Connected
# ✅ Branch Protection Rules: Respected
# ✅ Required Status Checks: Configured
# ⚠️  Warning: 3 stale feature branches found
# ❌ Error: Local develop branch is 5 commits behind origin
```

## Error Handling & Recovery

### Common Scenarios

#### Dirty Working Directory
```bash
# When trying to switch branches with uncommitted changes
claude issue start 127
# → Warning: Uncommitted changes detected
# → Options: [Stash] [Commit] [Cancel] [Force]
# → Auto-stash and apply after branch creation
```

#### Branch Already Exists
```bash
# If branch name conflicts with existing branch
claude issue start 123
# → Branch feature/ISSUE-123-user-auth already exists
# → Options: [Switch to existing] [Create variant] [Force recreate]
# → Intelligent suffix: feature/ISSUE-123-user-auth-2
```

#### Network Issues
```bash
# When GitHub API is unreachable
claude issue start 125
# → GitHub API unreachable, working offline
# → Created local branch: feature/ISSUE-125-offline-branch
# → Will sync with GitHub when connection restored
# → Run 'claude sync' to push when online
```

#### Permission Issues
```bash
# When lacking GitHub repository permissions
claude issue finish
# → Error: Insufficient permissions to create Pull Request
# → Alternative: Push branch and provide GitHub URL for manual PR
# → Pushed to: https://github.com/andrekirst/kgv_migration/tree/feature/ISSUE-123
```

## Integration with Development Tools

### VS Code Integration
The Git Flow system integrates with VS Code through:
- **Status Bar**: Shows current issue and branch status
- **Command Palette**: Quick access to Git Flow commands
- **Branch Switcher**: Enhanced with issue information
- **Commit Templates**: Auto-populated with issue references

### CI/CD Pipeline Integration
```yaml
# .github/workflows/feature-branch.yml
name: Feature Branch CI
on:
  push:
    branches: [ 'feature/**', 'bugfix/**' ]
  pull_request:
    branches: [ develop ]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Extract Issue Number
      id: issue
      run: echo "number=$(echo ${{ github.ref }} | grep -oP 'ISSUE-\K\d+')" >> $GITHUB_OUTPUT
    
    - name: Update Issue Status
      uses: actions/github-script@v7
      with:
        script: |
          github.rest.issues.createComment({
            issue_number: ${{ steps.issue.outputs.number }},
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: '🔄 Tests running for this issue on branch `${{ github.ref_name }}`'
          });
```

## Team Collaboration Features

### Code Review Automation
- **Auto-assign reviewers** based on code ownership
- **Review reminders** for stale PRs
- **Conflict detection** before merging
- **Automated testing** integration

### Team Communication
```bash
# Notify team about release
claude flow release start 1.2.0 --notify-team
# → Sends Slack/Teams notification about upcoming release
# → Creates announcement issue with release notes template

# Emergency hotfix notification
claude flow hotfix start 1.1.1 --emergency
# → Immediate notifications to on-call team
# → Creates incident tracking issue
# → Bypasses normal review process (if configured)
```

## Advanced Configuration

### Custom Workflows per Repository
```yaml
# .claude/workflows/kgv-migration.yml
name: "KGV Migration Workflow"
description: "Custom workflow for KGV modernization project"

branches:
  main:
    protection:
      required_reviews: 2
      dismiss_stale_reviews: true
      require_code_owner_reviews: true
  
  develop:
    auto_merge_features: true
    run_tests_before_merge: true

issue_types:
  enhancement:
    branch_prefix: "feature/"
    labels: ["enhancement", "needs-review"]
  
  bug:
    branch_prefix: "bugfix/"
    labels: ["bug", "priority-high"]
    auto_assign: ["@maintainer"]

automation:
  slack_webhook: "${SLACK_WEBHOOK_URL}"
  teams_webhook: "${TEAMS_WEBHOOK_URL}"
  
  notifications:
    pr_created: ["slack", "email"]
    release_finished: ["teams", "slack"]
    hotfix_deployed: ["pager", "slack"]
```

This Git Flow system provides a comprehensive, automated workflow that scales from individual development to team collaboration while maintaining flexibility and robustness.

---

# 🤖 Sub-Agent Automation System

## Proactive Sub-Agent Usage

Claude Code MUST automatically use specialized sub-agents when tasks match their expertise. This ensures optimal results and consistency across the project.

### 🔄 Automatic Agent Selection Rules

#### **ALWAYS Use These Agents Proactively:**

##### 1. **@code-reviewer** - MANDATORY after code writing
```
WHEN: Any significant code has been written or modified
ACTION: Immediately invoke code-reviewer for quality, security, and maintainability review
PRIORITY: Critical - NEVER skip this step
```

##### 2. **@architect-reviewer** - MANDATORY after architectural changes
```
WHEN: New services, APIs, architecture patterns, or structural changes
ACTION: Invoke architect-reviewer to ensure SOLID principles and proper layering
PRIORITY: Critical - Required for architectural consistency
```

##### 3. **@debugger** - MANDATORY when encountering issues
```
WHEN: Errors, test failures, unexpected behavior, or troubleshooting needed
ACTION: Immediately invoke debugger for systematic problem resolution
PRIORITY: Critical - Use proactively, don't wait for issues to escalate
```

##### 4. **@test-automator** - MANDATORY for test coverage
```
WHEN: New features, components, or modules are implemented
ACTION: Invoke test-automator for comprehensive test suite creation
PRIORITY: High - Essential for quality assurance
```

##### 5. **@security-auditor** - MANDATORY for security reviews
```
WHEN: Authentication, authorization, data handling, or API endpoints
ACTION: Invoke security-auditor for vulnerability assessment
PRIORITY: Critical - Required for production readiness
```

### 🎯 Task-Specific Agent Mapping

#### **Frontend Development:**
```yaml
Tasks: React components, Next.js pages, UI/UX implementation, styling
Primary: @frontend-developer
Secondary: @nextjs-expert, @tailwind-expert, @ui-ux-designer
Always Follow With: @code-reviewer, @test-automator
```

#### **Backend Development:**
```yaml
Tasks: .NET APIs, controllers, services, business logic
Primary: @backend-architect, @csharp-expert
Secondary: @rest-expert, @api-documenter
Always Follow With: @code-reviewer, @security-auditor, @test-automator
```

#### **Database Operations:**
```yaml
Tasks: Schema changes, migrations, query optimization
Primary: @data-engineer, @sql-pro
Secondary: @database-optimizer, @database-admin
Always Follow With: @code-reviewer, @performance-engineer
```

#### **Container & Infrastructure:**
```yaml
Tasks: Docker, Kubernetes, deployment configurations
Primary: @docker-expert, @kubernetes-expert
Secondary: @deployment-engineer, @devops-troubleshooter
Always Follow With: @security-auditor, @performance-engineer
```

#### **Legacy Migration:**
```yaml
Tasks: Legacy system integration, modernization, migration
Primary: @legacy-modernizer
Secondary: @architect-reviewer, @csharp-expert
Always Follow With: @security-auditor, @test-automator
```

#### **Performance & Optimization:**
```yaml
Tasks: Performance issues, optimization, bottleneck analysis
Primary: @performance-engineer
Secondary: @database-optimizer, @csharp-expert
Always Follow With: @test-automator, @debugger
```

### 🚨 Critical Automation Triggers

#### **Production Issues (Emergency Response):**
```
Trigger: Production errors, system outages, critical bugs
Sequence:
1. @devops-troubleshooter (immediate response)
2. @error-detective (root cause analysis)
3. @debugger (systematic debugging)
4. @security-auditor (security impact assessment)
5. @test-automator (regression testing)
```

#### **New Feature Implementation:**
```
Trigger: Feature development request
Sequence:
1. @backend-architect OR @frontend-developer (based on scope)
2. @ui-ux-designer (if UI changes)
3. @security-auditor (security review)
4. @test-automator (test coverage)
5. @code-reviewer (final review)
6. @architect-reviewer (architectural consistency)
```

#### **Database Schema Changes:**
```
Trigger: Database modifications, migrations
Sequence:
1. @data-engineer (schema design)
2. @sql-pro (query optimization)
3. @database-admin (deployment strategy)
4. @performance-engineer (performance impact)
5. @test-automator (migration testing)
```

### 📋 Agent Selection Matrix

| Task Category | Primary Agent | Secondary Agents | Mandatory Follow-up |
|---------------|---------------|------------------|-------------------|
| **API Development** | @backend-architect | @rest-expert, @csharp-expert | @code-reviewer, @security-auditor |
| **Frontend Components** | @frontend-developer | @nextjs-expert, @tailwind-expert | @code-reviewer, @test-automator |
| **Database Migration** | @data-engineer | @sql-pro, @legacy-modernizer | @code-reviewer, @performance-engineer |
| **Container Setup** | @docker-expert | @kubernetes-expert, @deployment-engineer | @security-auditor, @devops-troubleshooter |
| **Performance Issues** | @performance-engineer | @database-optimizer, @csharp-expert | @test-automator, @debugger |
| **Security Reviews** | @security-auditor | @backend-architect, @database-admin | @test-automator, @code-reviewer |
| **Legacy Integration** | @legacy-modernizer | @architect-reviewer, @csharp-expert | @security-auditor, @test-automator |
| **CI/CD Pipeline** | @github-actions-expert | @deployment-engineer, @docker-expert | @security-auditor, @test-automator |
| **Error Investigation** | @debugger | @error-detective, @devops-troubleshooter | @test-automator, @code-reviewer |
| **Architecture Review** | @architect-reviewer | @backend-architect, @performance-engineer | @security-auditor, @code-reviewer |

### 🎯 Context-Aware Agent Selection

#### **File-Based Triggers:**
```yaml
# Automatically invoke based on file types being worked on:

"*.cs": [@csharp-expert, @backend-architect]
"*.tsx, *.jsx": [@frontend-developer, @nextjs-expert]
"*.sql": [@sql-pro, @data-engineer]
"Dockerfile*": [@docker-expert]
"*.yml, *.yaml" (k8s): [@kubernetes-expert]
"*.md" (docs): [@api-documenter]
"*.css, *.scss": [@css-expert, @tailwind-expert]
"*.test.*, *.spec.*": [@test-automator]
".github/workflows/*": [@github-actions-expert]
```

#### **Keyword Triggers:**
```yaml
# Automatically invoke when these keywords appear in user requests:

"performance", "slow", "optimization": [@performance-engineer]
"security", "vulnerability", "authentication": [@security-auditor]
"error", "bug", "failing", "broken": [@debugger, @error-detective]
"database", "migration", "schema": [@data-engineer, @sql-pro]
"container", "docker", "kubernetes": [@docker-expert, @kubernetes-expert]
"frontend", "UI", "component", "styling": [@frontend-developer, @ui-ux-designer]
"API", "endpoint", "REST", "backend": [@backend-architect, @rest-expert]
"legacy", "migration", "modernization": [@legacy-modernizer]
"test", "testing", "coverage": [@test-automator]
"deployment", "CI/CD", "pipeline": [@deployment-engineer, @github-actions-expert]
```

### ⚡ Immediate Action Rules

#### **NEVER Skip These Agents:**
1. **@code-reviewer** - After ANY code writing/modification
2. **@security-auditor** - For ANY authentication, API, or data handling code
3. **@test-automator** - For ANY new functionality
4. **@debugger** - When ANY errors or unexpected behavior occurs
5. **@architect-reviewer** - After ANY architectural changes

#### **Chain Reactions:**
```
Database Change → @data-engineer → @sql-pro → @performance-engineer → @test-automator
API Creation → @backend-architect → @rest-expert → @security-auditor → @code-reviewer
UI Component → @frontend-developer → @tailwind-expert → @test-automator → @code-reviewer
Container Build → @docker-expert → @security-auditor → @performance-engineer
```

### 🔍 Quality Gates

Each sub-agent must complete their review before proceeding:

```yaml
Quality_Gates:
  Code_Quality: [@code-reviewer] # MANDATORY
  Security: [@security-auditor] # MANDATORY for production code
  Performance: [@performance-engineer] # For critical paths
  Architecture: [@architect-reviewer] # For structural changes
  Testing: [@test-automator] # For new features
```

### 📊 Agent Performance Tracking

Monitor and ensure agents are being used appropriately:

```yaml
Metrics:
  - Agent_Usage_Rate: Track frequency of proactive agent usage
  - Quality_Improvement: Measure code quality before/after agent reviews
  - Issue_Prevention: Count issues caught by proactive agent usage
  - Response_Time: Track how quickly agents respond to triggers
```

### 🎯 Success Criteria

The sub-agent system is working effectively when:
- ✅ **100% Code Review Coverage**: Every code change reviewed by @code-reviewer
- ✅ **Proactive Security**: @security-auditor invoked for all security-relevant code
- ✅ **Comprehensive Testing**: @test-automator ensures >80% test coverage
- ✅ **Zero Architecture Drift**: @architect-reviewer maintains consistency
- ✅ **Rapid Issue Resolution**: @debugger responds to all errors immediately

## 💡 Best Practices for Sub-Agent Usage

1. **Always Use Multiple Agents**: Complex tasks benefit from multiple perspectives
2. **Follow Agent Chains**: Some agents work better in sequence
3. **Don't Skip Reviews**: Quality gates are mandatory, not optional
4. **Use Context**: File types and keywords should trigger appropriate agents
5. **Learn from Agents**: Incorporate their recommendations into future work

This automated sub-agent system ensures consistent, high-quality outcomes across all aspects of the KGV migration project.

---

# 🚨 MANDATORY Git Flow Workflow

## ⚠️ CRITICAL RULE: Branch Creation BEFORE Development

**BEFORE starting ANY development work, you MUST:**

### 1. Create Feature Branch from GitHub Issue (MANDATORY)
```bash
# Option 1: Create branch from issue number
claude issue start 4
# → Creates: feature/ISSUE-4-container-infrastructure-docker-kubernetes

# Option 2: Create branch from issue URL  
claude issue start https://github.com/andrekirst/kgv_migration/issues/4

# Option 3: Manual branch creation (if claude commands not available)
git checkout develop
git pull origin develop  
git checkout -b feature/ISSUE-4-container-infrastructure-docker-kubernetes
git push -u origin feature/ISSUE-4-container-infrastructure-docker-kubernetes
```

### 2. Link Branch to Issue on GitHub
- Branch wird automatisch mit Issue verknüpft
- Issue Status wird auf "In Progress" gesetzt
- GitHub Tracking wird aktiviert

### 3. Local Development Setup
```bash
# Verify correct branch
git branch --show-current
# Should show: feature/ISSUE-4-container-infrastructure-docker-kubernetes

# Ensure clean working directory
git status
# Should show: nothing to commit, working tree clean
```

## 🔒 WORKFLOW ENFORCEMENT

### NEVER Start Development Without:
- ✅ **Feature Branch Created**: from GitHub Issue
- ✅ **Branch Checked Out Locally**: correct branch active
- ✅ **Issue Linked**: GitHub Issue shows "In Progress"
- ✅ **Clean Working Directory**: no uncommitted changes

### Development Process:
1. **Create Feature Branch** (MANDATORY)
2. **Make Commits** with conventional commit messages
3. **Push Regularly** to remote branch
4. **Create Pull Request** when feature complete
5. **Code Review** and merge to develop
6. **Branch Cleanup** after merge

### Commit Message Format:
```bash
git commit -m "feat(docker): implement docker-compose infrastructure

- Add development docker-compose.yml with all services
- Configure postgres, redis, nginx reverse proxy  
- Add health checks and proper networking
- Support hot reload for local development

Closes #4"
```

## 📋 Pre-Development Checklist

Before ANY coding work:
- [ ] GitHub Issue exists and is assigned
- [ ] Feature branch created from issue
- [ ] Local branch checked out
- [ ] Working directory is clean
- [ ] Issue status updated to "In Progress"

**FAILURE TO FOLLOW THIS WORKFLOW WILL RESULT IN:**
- ❌ Merge conflicts
- ❌ Git history problems  
- ❌ Lost work
- ❌ CI/CD pipeline failures
- ❌ Team collaboration issues

This workflow is **NON-NEGOTIABLE** and must be followed for every development task.