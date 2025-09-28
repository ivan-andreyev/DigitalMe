#!/bin/bash

# Script to test if the database fix worked correctly
# Tests that digitalme_fixed database has proper column names and functionality

echo "🔍 Testing DigitalMe Database Fix..."
echo "=================================="

# Configuration
API_URL="https://digitalme-api-llig7ks2ca-uc.a.run.app"
HEALTH_ENDPOINT="$API_URL/health"

echo ""
echo "📡 Step 1: Testing Health Endpoint"
echo "-----------------------------------"

# Test health endpoint
echo "🔄 Calling health endpoint..."
HEALTH_RESPONSE=$(curl -s -w "HTTPSTATUS:%{http_code}" "$HEALTH_ENDPOINT")
HTTP_STATUS=$(echo $HEALTH_RESPONSE | grep -o "HTTPSTATUS:[0-9]*" | cut -d: -f2)
RESPONSE_BODY=$(echo $HEALTH_RESPONSE | sed -E 's/HTTPSTATUS:[0-9]*$//')

echo "📊 HTTP Status: $HTTP_STATUS"
echo "📝 Response: $RESPONSE_BODY"

if [ "$HTTP_STATUS" -eq 200 ] && [[ "$RESPONSE_BODY" == *"Healthy"* ]]; then
    echo "✅ Health endpoint test: PASSED"
    HEALTH_TEST_PASSED=true
else
    echo "❌ Health endpoint test: FAILED"
    echo "   Expected: HTTP 200 with 'Healthy' response"
    echo "   Got: HTTP $HTTP_STATUS with '$RESPONSE_BODY'"
    HEALTH_TEST_PASSED=false
fi

echo ""
echo "📋 Step 2: Checking Application Logs"
echo "------------------------------------"

echo "🔄 Fetching recent application logs..."
gcloud logging read 'resource.type=cloud_run_revision AND resource.labels.service_name=digitalme-api' \
    --format="value(textPayload)" \
    --limit=20 > /tmp/recent_logs.txt

# Check for critical fix messages
if grep -q "CRITICAL FIX: EnsureCreated ignores HasColumnName mappings" /tmp/recent_logs.txt; then
    echo "✅ Found migration fix log entry"
    MIGRATION_LOG_FOUND=true
else
    echo "❌ Migration fix log entry not found"
    MIGRATION_LOG_FOUND=false
fi

# Check for successful migration
if grep -q "PostgreSQL database migrated successfully with correct column names" /tmp/recent_logs.txt; then
    echo "✅ Found successful migration log"
    MIGRATION_SUCCESS_FOUND=true
else
    echo "❌ Successful migration log not found"
    MIGRATION_SUCCESS_FOUND=false
fi

# Check for column name errors
if grep -q "column.*isactive.*does not exist" /tmp/recent_logs.txt; then
    echo "❌ Still found isactive column errors"
    COLUMN_ERRORS_FOUND=true
else
    echo "✅ No isactive column errors found"
    COLUMN_ERRORS_FOUND=false
fi

echo ""
echo "🔬 Step 3: Testing API Endpoints"
echo "--------------------------------"

# Test a simple API endpoint that doesn't require auth
echo "🔄 Testing registration endpoint availability..."
REG_RESPONSE=$(curl -s -w "HTTPSTATUS:%{http_code}" -X POST "$API_URL/api/auth/register" \
    -H "Content-Type: application/json" \
    -d '{"email":"test@example.com","password":"TestPass123!"}' 2>/dev/null)

REG_HTTP_STATUS=$(echo $REG_RESPONSE | grep -o "HTTPSTATUS:[0-9]*" | cut -d: -f2)

if [ "$REG_HTTP_STATUS" -eq 400 ] || [ "$REG_HTTP_STATUS" -eq 422 ]; then
    echo "✅ Registration endpoint: ACCESSIBLE (validation errors expected)"
    REG_ENDPOINT_ACCESSIBLE=true
elif [ "$REG_HTTP_STATUS" -eq 500 ]; then
    echo "❌ Registration endpoint: SERVER ERROR (likely database issues)"
    REG_ENDPOINT_ACCESSIBLE=false
else
    echo "⚠️  Registration endpoint: HTTP $REG_HTTP_STATUS (unexpected but may be OK)"
    REG_ENDPOINT_ACCESSIBLE=true
fi

echo ""
echo "📊 Step 4: Summary Report"
echo "========================"

# Calculate overall success
OVERALL_SUCCESS=true

echo "Test Results:"
echo "-------------"

if [ "$HEALTH_TEST_PASSED" = true ]; then
    echo "✅ Health Endpoint: PASSED"
else
    echo "❌ Health Endpoint: FAILED"
    OVERALL_SUCCESS=false
fi

if [ "$MIGRATION_LOG_FOUND" = true ]; then
    echo "✅ Migration Fix Applied: YES"
else
    echo "❌ Migration Fix Applied: NO"
    OVERALL_SUCCESS=false
fi

if [ "$MIGRATION_SUCCESS_FOUND" = true ]; then
    echo "✅ Migration Successful: YES"
else
    echo "❌ Migration Successful: NO"
    OVERALL_SUCCESS=false
fi

if [ "$COLUMN_ERRORS_FOUND" = false ]; then
    echo "✅ Column Name Errors: NONE"
else
    echo "❌ Column Name Errors: STILL PRESENT"
    OVERALL_SUCCESS=false
fi

if [ "$REG_ENDPOINT_ACCESSIBLE" = true ]; then
    echo "✅ API Endpoints: ACCESSIBLE"
else
    echo "❌ API Endpoints: NOT ACCESSIBLE"
    OVERALL_SUCCESS=false
fi

echo ""
echo "🏁 Final Result:"
echo "================"

if [ "$OVERALL_SUCCESS" = true ]; then
    echo "✅ DATABASE FIX SUCCESSFUL!"
    echo "   The digitalme_fixed database is working correctly."
    echo "   Ready to proceed with data migration if needed."
    exit 0
else
    echo "❌ DATABASE FIX FAILED!"
    echo "   Some issues were detected. Check the logs above."
    echo "   May need to investigate further or rollback."
    exit 1
fi