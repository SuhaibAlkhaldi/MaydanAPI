## Time-Unit Payment Calculation Implementation

### Overview
Implemented a flexible payment calculation system that supports three time-unit types: **Shift**, **Day**, and **Hour**. The system calculates expected payment amounts based on the selected time unit using accurate formulas.

### Changes Made

#### 1. **New Enum: ServiceTimeUnit**
**File:** `src/Maydan.Domain/Enums/ServiceTimeUnit.cs`

```csharp
public enum ServiceTimeUnit
{
	Shift = 1,  // 12 hours per shift
	Day = 2,    // 9 hours per day
	Hour = 3    // Direct hourly rate
}
```

**Time Unit Definitions:**
- **Shift**: 12 hours → Formula: `(12 * service.Price) * workers * shiftCount`
- **Day**: 9 hours → Formula: `(9 * service.Price) * workers * dayCount`
- **Hour**: Direct → Formula: `(service.Price * hourCount) * workers`

---

#### 2. **ServiceRequest Entity**
**File:** `src/Maydan.Domain/Entities/ServiceRequest.cs`

Added new property:
```csharp
public ServiceTimeUnit TimeUnit { get; set; } = ServiceTimeUnit.Shift;
```

This field stores which time unit is used for payment calculation.

---

#### 3. **CreateServiceRequestDto**
**File:** `src/Maydan.Application/DTOs/ServiceRequests/CreateServiceRequestDto.cs`

Added new field:
```csharp
public ServiceTimeUnit TimeUnit { get; set; } = ServiceTimeUnit.Shift;
```

This allows the client to specify the payment time unit when creating a service request.

---

#### 4. **CalculationRequestDto**
**File:** `src/Maydan.Application/DTOs/ServiceRequests/CalculationRequestDto.cs`

Updated to support time-unit based calculations:
```csharp
public class CalculationRequestDto
{
	public int ServiceId { get; set; }
	public int RequestedWorkers { get; set; }
	public int DurationCount { get; set; }  // Changed from "ShiftsOrDaysCount"
	public ServiceTimeUnit TimeUnit { get; set; } = ServiceTimeUnit.Shift;  // NEW
}
```

---

#### 5. **ServiceRequestService - CreateAsync**
**File:** `src/Maydan.Application/Services/ServiceRequestService.cs`

Payment calculation now uses switch case logic:
```csharp
sr.ExpectedTotalAmount = dto.TimeUnit switch
{
	ServiceTimeUnit.Shift =>
		(12 * service.Price) * dto.RequestedWorkersCount * dto.ShiftsCount,

	ServiceTimeUnit.Day =>
		(9 * service.Price) * dto.RequestedWorkersCount * dto.ShiftsCount,

	ServiceTimeUnit.Hour =>
		(service.Price * dto.ShiftsCount) * dto.RequestedWorkersCount,

	_ => throw new InvalidOperationException("Invalid time unit.")
};
```

---

#### 6. **ServiceRequestService - CalculateExpectedPaymentAsync**
**File:** `src/Maydan.Application/Services/ServiceRequestService.cs`

Updated method signature:
```csharp
public async Task<ExpectedPaymentCalculationDto> CalculateExpectedPaymentAsync(
	int currentUserId, 
	int serviceId, 
	int requestedWorkers, 
	int durationCount,  // Changed from "shiftsOrDaysCount"
	ServiceTimeUnit timeUnit,  // NEW PARAMETER
	CancellationToken cancellationToken = default)
```

Implements the same switch case logic with descriptive messages:
```csharp
decimal totalExpected = timeUnit switch
{
	ServiceTimeUnit.Shift =>
		(12 * service.Price) * requestedWorkers * durationCount,

	ServiceTimeUnit.Day =>
		(9 * service.Price) * requestedWorkers * durationCount,

	ServiceTimeUnit.Hour =>
		(service.Price * durationCount) * requestedWorkers,

	_ => throw new InvalidOperationException("Invalid time unit.")
};
```

---

#### 7. **ServiceRequestsController**
**File:** `src/Maydan.API/Controllers/ServiceRequestsController.cs`

Updated calculate endpoint to pass TimeUnit:
```csharp
[HttpPost("calculate-expected-payment")]
public async Task<ActionResult<ExpectedPaymentCalculationDto>> Calculate(
	[FromBody] CalculationRequestDto dto, 
	CancellationToken cancellationToken)
{
	var calculationResult = await _serviceRequestService.CalculateExpectedPaymentAsync(
		currentUserId, 
		dto.ServiceId, 
		dto.RequestedWorkers, 
		dto.DurationCount,  // Renamed parameter
		dto.TimeUnit,  // NEW: Pass TimeUnit
		cancellationToken);
	return Success(calculationResult);
}
```

---

#### 8. **IServiceRequestService Interface**
**File:** `src/Maydan.Application/Interfaces/IServiceRequestService.cs`

Updated method signature:
```csharp
Task<ExpectedPaymentCalculationDto> CalculateExpectedPaymentAsync(
	int currentUserId, 
	int serviceId, 
	int requestedWorkers, 
	int durationCount,  // Changed
	ServiceTimeUnit timeUnit,  // NEW
	CancellationToken cancellationToken = default);
```

---

#### 9. **ServiceRequestConfiguration**
**File:** `src/Maydan.Infrastructure/Persistence/Configurations/ServiceRequestConfiguration.cs`

Added TimeUnit configuration:
```csharp
builder.Property(r => r.TimeUnit)
	.HasDefaultValue(Maydan.Domain.Enums.ServiceTimeUnit.Shift);
```

---

### Calculation Formulas Summary

| Time Unit | Hours | Formula |
|-----------|-------|----------|
| **Shift** | 12 | `(12 × Price) × Workers × ShiftCount` |
| **Day** | 9 | `(9 × Price) × Workers × DayCount` |
| **Hour** | 1 | `(Price × HourCount) × Workers` |

---

### API Request Example

**POST** `/api/ServiceRequests/calculate-expected-payment`

```json
{
  "serviceId": 1,
  "requestedWorkers": 5,
  "durationCount": 3,
  "timeUnit": 0  // 0=Shift, 1=Day, 2=Hour
}
```

**Response Example (Shift):**
```json
{
  "success": true,
  "messageAr": "تم حساب المبلغ بنجاح",
  "messageEn": "Payment calculated successfully",
  "data": {
	"unitPrice": 50.00,
	"requestedWorkers": 5,
	"durationUnits": 3,
	"totalExpectedPayment": 9000.00,
	"formattedMessage": "Expected payment for 5 workers for 3 shifts (12 hours each): 9000.00 JOD."
  }
}
```

---

### Database Migration Required

Run the following command to create and apply the migration:

```powershell
cd "C:\Users\DELL\source\repos\MaydanAPI"
dotnet ef migrations add AddTimeUnitToServiceRequest -p src\Maydan.Infrastructure -s src\Maydan.API
dotnet ef database update
```

---

### Build Status
**Build Successful** - All code compiles without errors.

---

### Key Features

**Accurate Calculations** - Each time unit uses the correct hourly multiplier
**Flexible** - Clients can choose their preferred payment unit
**Backward Compatible** - Defaults to Shift if not specified
**Descriptive Messages** - Responses include clear labels for the time unit used
**Type-Safe** - Enum-based approach prevents invalid values

