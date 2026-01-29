#!/bin/bash

ENDPOINT="http://localhost:8000"

aws dynamodb create-table \
    --endpoint-url $ENDPOINT \
    --table-name Local_Users \
    --attribute-definitions \
        AttributeName=Id,AttributeType=S \
        AttributeName=Email,AttributeType=S \
    --key-schema AttributeName=Id,KeyType=HASH \
    --global-secondary-indexes \
        "[{\"IndexName\": \"EmailIndex\", \"KeySchema\": [{\"AttributeName\": \"Email\", \"KeyType\": \"HASH\"}], \"Projection\": {\"ProjectionType\": \"ALL\"}, \"ProvisionedThroughput\": {\"ReadCapacityUnits\": 5, \"WriteCapacityUnits\": 5}}]" \
    --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5 \
    --no-cli-pager

echo "Tables created successfully"
