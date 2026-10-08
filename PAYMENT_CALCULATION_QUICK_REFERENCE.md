##  Payment Calculation - Quick Reference

### TimeUnit Enum Values

```
0 = Shift (12 hours)
1 = Day (9 hours)
2 = Hour (Direct hourly rate)
```

---

### API Endpoint

**POST** `/api/ServiceRequests/calculate-expected-payment`

---

### Example: Shift Calculation

**Request:**
```json
{
  "serviceId": 1,
  "requestedWorkers": 5,
  "durationCount": 3,
  "timeUnit": 0
}
```

**Calculation:**
- Service Price: 50 JOD/hour
- Formula: (12 * 50) × 5 × 3 = 9,000 JOD
- Per worker per shift: 12 × 50 = 600 JOD
- Total for 5 workers × 3 shifts: 9,000 JOD

**Response:**
```json
{
  "unitPrice": 50,
  "requestedWorkers": 5,
  "durationUnits": 3,
  "totalExpectedPayment": 9000,
  "formattedMessage": "Expected payment for 5 workers for 3 shifts (12 hours each): 9000.00 JOD."
}
```

---

### Example: Day Calculation

**Request:**
```json
{
  "serviceId": 1,
  "requestedWorkers": 5,
  "durationCount": 3,
  "timeUnit": 1
}
```

**Calculation:**
- Service Price: 50 JOD/hour
- Formula: (9 * 50) × 5 × 3 = 6,750 JOD
- Per worker per day: 9 × 50 = 450 JOD
- Total for 5 workers × 3 days: 6,750 JOD

**Response:**
```json
{
  "unitPrice": 50,
  "requestedWorkers": 5,
  "durationUnits": 3,
  "totalExpectedPayment": 6750,
  "formattedMessage": "Expected payment for 5 workers for 3 days (9 hours each): 6750.00 JOD."
}
```

---

### Example: Hour Calculation

**Request:**
```json
{
  "serviceId": 1,
  "requestedWorkers": 5,
  "durationCount": 36,
  "timeUnit": 2
}
```

**Calculation:**
- Service Price: 50 JOD/hour
- Formula: (50 × 36) × 5 = 9,000 JOD
- Per worker: 50 × 36 = 1,800 JOD
- Total for 5 workers: 9,000 JOD

**Response:**
```json
{
  "unitPrice": 50,
  "requestedWorkers": 5,
  "durationUnits": 36,
  "totalExpectedPayment": 9000,
  "formattedMessage": "Expected payment for 5 workers for 36 hours: 9000.00 JOD."
}
```

---

### Creating a Service Request with TimeUnit

**POST** `/api/ServiceRequests`

```json
{
  "projectId": 5,
  "serviceId": 1,
  "cityId": 10,
  "startDate": "2024-11-01T08:00:00Z",
  "endDate": "2024-11-05T20:00:00Z",
  "timeUnit": 0,
  "shiftsCount": 3,
  "requestedWorkersCount": 5,
  "attendanceFrequency": 1,
  "additionalRequirements": "Must have experience",
  "unitPriceSnapshot": 50.00,
  "expectedTotalAmount": 9000.00,
  "idempotencyKey": "unique-request-123"
}
```

The `timeUnit` field specifies:
- **0** = Shift (12-hour shifts)
- **1** = Day (9-hour days)
- **2** = Hour (Direct hourly)

---

### Database Migration

```powershell
# Generate migration
dotnet ef migrations add AddTimeUnitToServiceRequest -p src\Maydan.Infrastructure -s src\Maydan.API

# Apply migration
dotnet ef database update
```

---

### Notes

Default time unit is **Shift (0)** if not specified
 All calculations are multiplied by `RequestedWorkers`
 `DurationCount` (in CreateDTO it's `ShiftsCount`) means:
   - For Shift: number of shifts
   - For Day: number of days
   - For Hour: number of hours

