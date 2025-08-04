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