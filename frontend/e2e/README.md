# KGV E2E Test Suite

Comprehensive end-to-end test suite for the KGV (Kleingartenverein) application's Bezirk management functionality using Playwright.

## Overview

This test suite provides comprehensive coverage of the Bezirk management workflow:

- **Navigation Flow Tests**: Dashboard → Bezirke Overview → Bezirk Creation
- **Form Creation Tests**: Validation, submission, error handling
- **List View Tests**: Data display, search, filtering, pagination
- **API Integration Tests**: Success/error scenarios, network conditions
- **Accessibility Tests**: Keyboard navigation, ARIA labels, screen reader support
- **Performance Tests**: Load times, large datasets, memory usage

## Test Structure

```
e2e/
├── fixtures/           # Test data and mock responses
│   ├── test-data.ts   # Bezirk test data generators
│   └── index.ts       # Fixture exports
├── pages/             # Page Object Models
│   ├── base-page.ts   # Common page functionality
│   ├── dashboard-page.ts
│   ├── bezirke-overview-page.ts
│   ├── bezirke-creation-page.ts
│   └── index.ts
├── tests/             # Test specifications
│   ├── bezirke-navigation.spec.ts
│   ├── bezirke-creation.spec.ts
│   └── bezirke-overview.spec.ts
├── utils/             # Test utilities and helpers
│   ├── test-helpers.ts
│   └── index.ts
├── global-setup.ts    # Global test setup
├── global-teardown.ts # Global test cleanup
├── setup.ts           # Per-project setup
├── cleanup.ts         # Per-project cleanup
└── README.md          # This file
```

## Prerequisites

1. **Node.js** (v18.17.0 or higher)
2. **npm** (v9.0.0 or higher)
3. **Frontend application** running on `http://localhost:3001`
4. **Backend API** running on `http://localhost:8080`
5. **Playwright browsers** installed

## Installation

1. Install dependencies:
```bash
npm install
```

2. Install Playwright browsers:
```bash
npx playwright install
```

## Running Tests

### Basic Test Execution

```bash
# Run all E2E tests
npm run e2e

# Run tests in UI mode (interactive)
npm run e2e:ui

# Run tests in headed mode (visible browser)
npx playwright test --headed

# Run specific test file
npx playwright test bezirke-navigation.spec.ts

# Run tests in specific browser
npx playwright test --project=chromium
```

### Test Filtering

```bash
# Run tests with specific tag
npx playwright test --grep "happy path"

# Skip tests with specific tag
npx playwright test --grep-invert "slow"

# Run only failed tests from last run
npx playwright test --last-failed
```

### Debug Mode

```bash
# Debug specific test
npx playwright test --debug bezirke-creation.spec.ts

# Debug with browser developer tools
PWDEBUG=1 npx playwright test
```

## Environment Configuration

Copy `.env.test` to `.env.local` and adjust as needed:

```bash
cp .env.test .env.local
```

Key environment variables:

- `PLAYWRIGHT_BASE_URL`: Frontend application URL (default: http://localhost:3001)
- `PLAYWRIGHT_API_BASE_URL`: Backend API URL (default: http://localhost:8080)
- `SKIP_API_TESTS`: Skip tests requiring API (default: false)
- `SKIP_SLOW_TESTS`: Skip performance/slow tests (default: false)
- `DEBUG`: Enable debug output (default: false)

## Test Categories

### 1. Navigation Tests (`bezirke-navigation.spec.ts`)

Tests navigation between different parts of the application:

- Dashboard to Bezirke overview
- Bezirke overview to creation form
- Breadcrumb navigation
- Browser back/forward buttons
- URL-based navigation
- Error handling during navigation

**Key Features:**
- Cross-browser compatibility
- Mobile responsive navigation
- Accessibility compliance
- Performance optimization

### 2. Form Creation Tests (`bezirke-creation.spec.ts`)

Comprehensive form testing including:

- Form rendering and initial state
- Client-side validation
- Server-side validation
- API error handling
- Form state management
- Data persistence

**Test Scenarios:**
- Happy path: Valid data submission
- Validation: Required fields, length limits, special characters
- Error handling: API failures, network issues, malformed data
- Edge cases: Boundary values, unicode characters
- Performance: Large data, repeated submissions

### 3. Overview/List Tests (`bezirke-overview.spec.ts`)

Tests for the Bezirke list view:

- Data display and formatting
- Statistics overview
- Search and filtering
- Pagination
- Loading and error states
- Empty state handling

**Features Tested:**
- Table functionality
- Data refresh
- Real-time updates
- Mobile responsiveness
- Accessibility features

## Page Object Model

Tests use the Page Object Model pattern for maintainability:

### BasePage
Common functionality shared across all pages:
- Navigation helpers
- Wait strategies
- Error handling
- Screenshot capture
- Accessibility checks

### BezirkeCreationPage
Form-specific functionality:
- Field interaction
- Validation testing
- Submission handling
- Error state management

### BezirkeOverviewPage
List view functionality:
- Data verification
- Search/filter operations
- Pagination handling
- Statistics validation

## Test Data Management

### BezirkTestData
Generates test data for various scenarios:
- Valid bezirk data
- Invalid data for validation testing
- Boundary value testing
- Special character testing

### MockApiData
Creates mock API responses:
- Successful responses
- Error responses
- Validation errors
- Large datasets

## Error Handling and Resilience

The test suite handles various failure scenarios:

### API Unavailability
- Mock 500/503 server errors
- Network timeouts
- Malformed responses
- Service degradation

### Application Errors
- JavaScript errors
- Network failures
- Toast notification issues
- White screen errors

### Recovery Testing
- Service restoration
- Data refresh after errors
- State management after failures

## Performance Testing

Performance tests verify:

- Page load times (< 5 seconds)
- Form submission speed
- Large dataset handling
- Memory usage patterns
- Network request optimization

## Accessibility Testing

Accessibility tests ensure:

- Proper heading structure
- ARIA labels and roles
- Keyboard navigation
- Screen reader compatibility
- Focus management
- Color contrast (where applicable)

## CI/CD Integration

Tests are configured for continuous integration:

### GitHub Actions
```yaml
- name: Run E2E Tests
  run: |
    npm run build
    npm run start &
    npm run e2e
```

### Test Reports
- HTML reports: `e2e-test-results/html-report/`
- JSON results: `e2e-test-results/test-results.json`
- JUnit XML: `e2e-test-results/results.xml`

## Debugging Failed Tests

### Trace Viewer
```bash
npx playwright show-trace e2e-test-results/trace.zip
```

### Screenshots
Failed tests automatically capture:
- Full page screenshots
- Element-specific captures
- Network request logs
- Console output

### Debug Mode
```bash
# Interactive debugging
npx playwright test --debug

# Step-by-step execution
PWDEBUG=1 npx playwright test
```

## Known Issues and Workarounds

### Backend API 500 Errors
The application currently has known issues with 500 errors during bezirk creation. Tests handle this by:
- Mocking successful responses for form testing
- Testing error scenarios separately
- Using retry mechanisms for flaky network calls

### Toast Notifications
Toast import issues may prevent success notifications. Tests account for this by:
- Checking for navigation as success indicator
- Looking for multiple success indicators
- Providing fallback assertions

## Best Practices

### Test Organization
- Group related tests in describe blocks
- Use descriptive test names
- Keep tests independent
- Clean up test data after each test

### Page Interactions
- Use semantic selectors over CSS selectors
- Wait for elements to be ready before interaction
- Handle loading states explicitly
- Use proper error handling

### Data Management
- Generate unique test data
- Mock API responses for consistency
- Clean up created data
- Use fixtures for reusable data

### Assertions
- Use specific assertions over generic ones
- Verify user-visible behavior
- Check multiple success indicators
- Include negative test cases

## Contributing

When adding new tests:

1. Follow the existing Page Object Model structure
2. Add appropriate test data fixtures
3. Include both positive and negative scenarios
4. Test error handling paths
5. Verify accessibility compliance
6. Update documentation

## Troubleshooting

### Common Issues

**Tests fail with "Target page is closed"**
- Check if navigation is happening too quickly
- Add proper wait conditions
- Verify page lifecycle management

**API tests are flaky**
- Increase timeouts for slow CI environments
- Add retry mechanisms for network calls
- Mock responses for consistency

**Element not found errors**
- Verify selectors match current implementation
- Check if elements are in viewport
- Wait for proper loading state

**Performance tests fail in CI**
- Adjust timeouts for CI environment
- Use relative performance thresholds
- Consider environment-specific configurations

### Support

For issues with the test suite:
1. Check the test output and traces
2. Verify environment configuration
3. Review recent application changes
4. Check browser and Playwright versions
5. Consult the team documentation

## Future Improvements

Planned enhancements:
- Visual regression testing
- API contract testing
- Load testing integration
- Cross-device testing
- Automated accessibility audits