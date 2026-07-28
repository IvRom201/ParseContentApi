# Parse Content API

A REST API built with ASP.NET Core Minimal APIs and .NET 10.

The application accepts Base64-encoded content, decodes it as UTF-8 text, parses it according to the selected format, and returns the processed data in a unified JSON response.

## Supported formats

The API currently supports:

- `CSV`
- `INTERNAL_JSON`

## Technologies

- C#
- .NET 10
- ASP.NET Core Minimal APIs
- System.Text.Json
- xUnit
- Microsoft.AspNetCore.Mvc.Testing
- OpenAPI

## Project structure

```text
ParseContentApi/
├── ParseContentApi/
│   ├── Contracts/
│   ├── Endpoints/
│   ├── Exceptions/
│   ├── Infrastructure/
│   ├── Options/
│   ├── Services/
│   ├── Program.cs
│   └── ParseContentApi.csproj
│
├── ParseContentApi.Tests/
│   ├── Integration/
│   ├── Unit/
│   └── ParseContentApi.Tests.csproj
│
└── ParseContentApi.sln
```

## Requirements

Before running the application, make sure the following software is installed:

- .NET 10 SDK
- Git, if the repository is cloned from source

Verify the installed .NET SDK version:

```bash
dotnet --version
```

The output should indicate a .NET 10 SDK version.

## Running the application locally

Open a terminal in the solution directory.

Restore the project dependencies:

```bash
dotnet restore
```

Build the solution:

```bash
dotnet build
```

Run the API project:

```bash
dotnet run --project ParseContentApi/ParseContentApi.csproj
```

The application URL will be displayed in the terminal:

```text
https://localhost:5186
```

The exact port may differ depending on the local launch configuration.

## API endpoint

```http
POST /api/v1/parse-content
```

The request must use the following content type:

```http
Content-Type: application/json
```

## Request format

```json
{
  "type": "CSV",
  "content": "Base64-encoded content"
}
```

### Request properties

| Property | Type | Required | Description |
|---|---|---:|---|
| `type` | string | Yes | Content format. Supported values are `CSV` and `INTERNAL_JSON`. |
| `content` | string | Yes | Raw content encoded using Base64. The decoded text must use valid UTF-8 encoding. |

## CSV example

Original CSV content:

```csv
name,age
Alice,30
Bob,28
```

Base64-encoded value:

```text
bmFtZSxhZ2UKQWxpY2UsMzAKQm9iLDI4
```

Example request:

```bash
curl --request POST \
  --url https://localhost:7000/api/v1/parse-content \
  --header "Content-Type: application/json" \
  --data '{
    "type": "CSV",
    "content": "bmFtZSxhZ2UKQWxpY2UsMzAKQm9iLDI4"
  }'
```

Example response:

```json
{
  "status": "success",
  "type": "CSV",
  "processedCount": 2,
  "data": [
    {
      "name": "Alice",
      "age": "30"
    },
    {
      "name": "Bob",
      "age": "28"
    }
  ]
}
```

The first CSV row is treated as the header row. Each following row is converted into a JSON object.

The CSV parser supports:

- Quoted fields
- Commas inside quoted fields
- Escaped quotes
- Line breaks inside quoted fields
- Empty field values
- Header validation
- Column count validation

## INTERNAL_JSON example

Original JSON content:

```json
[
  {
    "id": 1,
    "name": "Alice"
  },
  {
    "id": 2,
    "name": "Bob"
  }
]
```

Base64-encoded value:

```text
W3siaWQiOjEsIm5hbWUiOiJBbGljZSJ9LHsiaWQiOjIsIm5hbWUiOiJCb2IifV0=
```

Example request:

```bash
curl --request POST \
  --url https://localhost:7000/api/v1/parse-content \
  --header "Content-Type: application/json" \
  --data '{
    "type": "INTERNAL_JSON",
    "content": "W3siaWQiOjEsIm5hbWUiOiJBbGljZSJ9LHsiaWQiOjIsIm5hbWUiOiJCb2IifV0="
  }'
```

Example response:

```json
{
  "status": "success",
  "type": "INTERNAL_JSON",
  "processedCount": 2,
  "data": [
    {
      "id": 1,
      "name": "Alice"
    },
    {
      "id": 2,
      "name": "Bob"
    }
  ]
}
```

A single JSON object is returned as one record. Primitive JSON values are wrapped in an object containing a `value` property so that the API response always uses a unified structure.

## Response format

A successful request returns:

```json
{
  "status": "success",
  "type": "CSV",
  "processedCount": 1,
  "data": [
    {
      "example": "value"
    }
  ]
}
```

### Response properties

| Property | Type | Description |
|---|---|---|
| `status` | string | Operation status. |
| `type` | string | Processed content format. |
| `processedCount` | integer | Number of parsed records. |
| `data` | array | Parsed records represented as JSON objects. |

## Error handling

The API uses the Problem Details format for parsing and processing errors.

Possible HTTP status codes include:

| Status code | Description |
|---:|---|
| `200 OK` | The content was decoded and parsed successfully. |
| `400 Bad Request` | The request is invalid, the format is unsupported, the Base64 value is invalid, or the decoded content cannot be parsed. |
| `413 Payload Too Large` | The request exceeds one of the configured processing limits. |
| `415 Unsupported Media Type` | The request does not use `Content-Type: application/json`. |
| `500 Internal Server Error` | An unexpected server error occurred. |

Example error response:

```json
{
  "type": "about:blank",
  "title": "Content parsing failed",
  "status": 400,
  "detail": "The content field does not contain valid Base64 data.",
  "instance": "/api/v1/parse-content",
  "traceId": "request-trace-identifier"
}
```

## Configuration

Content-processing limits are configured in `appsettings.json`:

```json
{
  "ContentParsing": {
    "MaxBase64Length": 14000000,
    "MaxDecodedBytes": 10000000,
    "MaxRecords": 10000,
    "MaxCsvColumns": 500,
    "MaxFieldLength": 1000000,
    "MaxJsonDepth": 64
  }
}
```

These limits protect the application from excessively large or deeply nested payloads.

## OpenAPI

When the application runs in the Development environment, the OpenAPI document is available at:

```text
/openapi/v1.json
```

For example:

```text
https://localhost:7000/openapi/v1.json
```

Replace the port with the port displayed in the application output.

## Running tests

Run all unit and integration tests from the solution directory:

```bash
dotnet test
```

Run only unit tests:

```bash
dotnet test --filter "FullyQualifiedName~Unit"
```

Run only integration tests:

```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

Run tests with detailed console output:

```bash
dotnet test --logger "console;verbosity=detailed"
```

The test suite covers:

- Base64 decoding
- UTF-8 validation
- Base64 and decoded-content size limits
- CSV parsing
- Quoted and escaped CSV values
- CSV validation errors
- Internal JSON parsing
- JSON depth and record limits
- Parser resolution
- Successful HTTP requests
- Request validation
- MIME-type validation
- Problem Details responses

## Design

The application follows SOLID principles and separates responsibilities between dedicated components:

- The endpoint handles the HTTP request and response flow.
- The Base64 decoder validates and decodes incoming content.
- Each supported format has its own parser implementation.
- The parser resolver selects the correct parser for the requested format.
- The request validator handles input validation.
- The global exception handler produces consistent error responses.

A new content format can be added by implementing `IContentParser` and registering the implementation in the dependency injection container.