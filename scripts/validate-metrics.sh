#!/bin/bash
set -e

echo "======================================"
echo "Metrics-Driven Development Validation"
echo "======================================"
echo ""

# Run all tests
echo "Running all tests..."
dotnet test --verbosity quiet --nologo

# Extract test results
TEST_RESULTS=$(dotnet test --verbosity quiet --nologo 2>&1)
TOTAL_TESTS=$(echo "$TEST_RESULTS" | grep "Total tests:" | awk '{print $3}')
PASSED_TESTS=$(echo "$TEST_RESULTS" | grep "Passed:" | awk '{print $2}')

echo ""
echo "======================================"
echo "Test Results"
echo "======================================"
echo "Total Tests: $TOTAL_TESTS"
echo "Passed: $PASSED_TESTS"

# Calculate test coverage percentage
if [ "$TOTAL_TESTS" -gt 0 ]; then
    TEST_COVERAGE=$(awk "BEGIN {printf \"%.1f\", ($PASSED_TESTS/$TOTAL_TESTS)*100}")
else
    TEST_COVERAGE=0
fi

echo "Test Coverage: ${TEST_COVERAGE}%"
echo ""

# Define metric targets
echo "======================================"
echo "Metric Targets"
echo "======================================"
echo "Target: Test Coverage = 100%"
echo "Target: Hallucination Rate = 0%"
echo "Target: Computer Login Answer Rate > 50%"
echo ""

# Validate metrics
echo "======================================"
echo "Metric Validation"
echo "======================================"

VALIDATION_FAILED=0

# Check test coverage
if [ "$(echo "$TEST_COVERAGE < 100" | bc)" -eq 1 ]; then
    echo "❌ Test Coverage: ${TEST_COVERAGE}% (Target: 100%)"
    VALIDATION_FAILED=1
else
    echo "✅ Test Coverage: ${TEST_COVERAGE}% (Target: 100%)"
fi

# Note: Hallucination rate and answer rates are validated within the MetricValidationTests
# Those tests will fail if metrics don't meet targets

if [ $VALIDATION_FAILED -eq 1 ]; then
    echo ""
    echo "❌ Metric validation FAILED"
    echo "Deployment blocked until metrics meet targets"
    exit 1
fi

echo ""
echo "✅ All metrics meet targets"
echo "Deployment approved"
exit 0
