# Controller & Meter Validation for Reading Forms

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enforce that generators must have an active controller and sites must have an active meter before allowing reading entry. Block form submission with clear error messages when requirements aren't met.

**Architecture:** Frontend validation blocks form rendering when controller/meter is missing. Backend validates required FK relationships on create. Previous reading logic enhanced to properly fallback through controller readings.

**Tech Stack:** React (frontend), ASP.NET Core / EF Core (backend)

---

## Task 1: Generator Reading Form — Block when no controller

**Files:**
- Modify: `frontend/src/pages/AddReading.jsx`

**Step 1: Add controller-required blocking state**

After the `controller` state fetch (line 72), add a computed flag:

```jsx
const hasController = !!controller;
```

**Step 2: Add blocking banner when no controller**

Replace the current warning text (lines 236-240) with a full blocking banner that prevents form interaction:

```jsx
{!hasController && (
  <div style={{
    background: 'rgba(220,53,69,0.1)',
    border: '1px solid var(--danger)',
    borderRadius: 8,
    padding: '12px 16px',
    marginBottom: 12,
  }}>
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
      <AlertCircle size={16} style={{ color: 'var(--danger)' }} />
      <span style={{ fontSize: 14, fontWeight: 700, color: 'var(--danger)' }}>
        {ar ? 'لا يمكن إدخال قراءة' : 'Cannot Enter Reading'}
      </span>
    </div>
    <div style={{ fontSize: 12, color: 'var(--danger)', lineHeight: 1.6 }}>
      {ar
        ? 'هذا المولد غير مرتبط بشاشة تحكم نشطة. يجب ربط المولد بشاشة تحكم أولاً قبل إدخال القراءة.'
        : 'This generator is not linked to an active control panel. Link the generator to a control panel before entering readings.'}
    </div>
  </div>
)}
```

**Step 3: Zero out previous readings when no controller**

Replace the prevPower/prevHours calculation (lines 100-101):

```jsx
const prevPower = hasController
  ? (lastReading?.currentEnergyKWh ?? generator?.lastPowerRecord ?? 0)
  : 0;
const prevHours = hasController
  ? (lastReading?.currentRunningHours ?? generator?.lastHoursRecord ?? 0)
  : 0;
```

**Step 4: Disable submit button when no controller**

Update the submit button `disabled` prop (line 461):

```jsx
disabled={saving || !hasController || !form.currentEnergyKWh || !form.currentRunningHours}
```

**Step 5: Also disable form inputs when no controller**

Add `disabled={!hasController}` to all form inputs (currentEnergyKWh, currentRunningHours, oilTypeUsed, oilAddedLiters, notes, etc.).

**Step 6: Add backend validation for controller requirement**

Modify `ReadingService.CreateAsync` to reject readings for generators without controllers:

```csharp
// After generator lookup (line 96), add:
var hasController = await _db.Controllers.AnyAsync(c => c.GeneratorId == dto.GeneratorId);
if (!hasController)
    throw new InvalidOperationException("Cannot create reading: generator is not linked to an active control panel.");
```

**Step 7: Test manually**
- Navigate to a generator without a controller → verify blocking banner shows, form disabled
- Navigate to a generator with a controller → verify form works normally

---

## Task 2: Generator Reading — Enhanced previous reading fallback

**Files:**
- Modify: `src/FuelTrack.Infrastructure/Services/GeneratorService.cs` (GetLastReadingAsync)

**Step 1: Update GetLastReadingAsync to check both DailyReadings and Controller**

The current logic (lines 560-578) already does this correctly:
1. First checks DailyReadings (which are tied to a specific generator)
2. Falls back to Controller readings

No changes needed to the backend — the existing fallback chain is:
`DailyReading (generator+date) → Controller (generator linked) → null`

The frontend already consumes this via `lastReading` state. The change in Task 1 (zeroing when no controller) handles the case where neither source exists.

**Step 2: Verify the flow**
- Generator with DailyReadings → uses DailyReading values ✓
- Generator with Controller but no DailyReadings → uses Controller values ✓
- Generator with neither → zeros out (Task 1 change) ✓

---

## Task 3: Site Reading Form — Block when no meter

**Files:**
- Modify: `frontend/src/pages/SubStationReading.jsx`

**Step 1: Add meter-required blocking state**

After the `gridMeter` state is set (line 62), add:

```jsx
const hasMeter = !!gridMeter;
```

**Step 2: Add blocking banner when no meter**

After the status alert (line 176), before the form, add:

```jsx
{!hasMeter && (
  <div style={{
    background: 'rgba(220,53,69,0.1)',
    border: '1px solid var(--danger)',
    borderRadius: 8,
    padding: '12px 16px',
    marginBottom: 20,
  }}>
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
      <AlertCircle size={16} style={{ color: 'var(--danger)' }} />
      <span style={{ fontSize: 14, fontWeight: 700, color: 'var(--danger)' }}>
        {ar ? 'لا يمكن إدخال قراءة' : 'Cannot Enter Reading'}
      </span>
    </div>
    <div style={{ fontSize: 12, color: 'var(--danger)', lineHeight: 1.6 }}>
      {ar
        ? 'هذا الموقع غير مرتبط بعداد نشط. يجب ربط الموقع بعداد أولاً قبل إدخال القراءة.'
        : 'This site is not linked to an active meter. Link the site to a meter before entering readings.'}
    </div>
  </div>
)}
```

**Step 3: Validate site number and meter number on submit**

In `handleSubmit` (line 99), add validation before the existing check:

```jsx
if (!subStationId) {
  setStatus({ type: 'error', msg: ar ? 'رقم الموقع مطلوب' : 'Site number is required' });
  return;
}
if (!gridMeter?.id) {
  setStatus({ type: 'error', msg: ar ? 'رقم العداد مطلوب' : 'Meter number is required' });
  return;
}
```

**Step 4: Show site number and meter number in the form display**

In the grid meter reading section, show the site number and meter number:

```jsx
{hasMeter && (
  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, marginBottom: 12 }}>
    <div style={{ background: 'var(--card-2)', borderRadius: 8, padding: '10px 14px' }}>
      <div style={{ ...labelStyle, marginBottom: 4 }}>{ar ? 'رقم الموقع' : 'Site Number'}</div>
      <div style={{ fontSize: 14, fontWeight: 700 }}>{subStationId || '—'}</div>
    </div>
    <div style={{ background: 'var(--card-2)', borderRadius: 8, padding: '10px 14px' }}>
      <div style={{ ...labelStyle, marginBottom: 4 }}>{ar ? 'رقم العداد' : 'Meter Number'}</div>
      <div style={{ fontSize: 14, fontWeight: 700 }}>{gridMeter?.meterName || gridMeter?.name || '—'}</div>
    </div>
  </div>
)}
```

**Step 5: Disable submit and inputs when no meter**

Update submit button disabled prop:

```jsx
disabled={saving || !hasMeter || !form.currentGridRecord || parseFloat(form.currentGridRecord) < prevGridVal}
```

Add `disabled={!hasMeter}` to form inputs.

**Step 6: Add backend validation for meter requirement**

Modify `SubStationReadingService.CreateAsync` to require a meter:

```csharp
// After meter resolution (line 53), add:
if (meterId is null || meterId == 0)
    throw new InvalidOperationException("Cannot create reading: site is not linked to an active meter. Link a meter to the site first.");
```

**Step 7: Add validation for subStationId in backend**

```csharp
// At the start of CreateAsync (after line 42), add:
if (dto.SubStationId <= 0)
    throw new ArgumentException("Site number (SubStationId) is required.");
```

**Step 8: Test manually**
- Navigate to a site without a meter → verify blocking banner shows, form disabled
- Navigate to a site with a meter → verify form works normally
- Try submitting without siteId → verify error message

---

## Task 4: Ensure CreateSubStationReadingDto validates required fields

**Files:**
- Modify: `src/FuelTrack.Application/DTOs/Readings/CreateSubStationReadingDto.cs` (if exists)
- Or check existing DTO in `src/FuelTrack.Application/DTOs/`

**Step 1: Check existing DTO structure**

Verify `CreateSubStationReadingDto` has required fields with proper validation attributes.

**Step 2: Add validation if missing**

Ensure `SubStationId` is required (not nullable) and `MeterId` is required.

---

## Task 5: Build verification

**Step 1: Build backend**
```bash
cd src/FuelTrack.API && dotnet build
```
Expected: 0 errors

**Step 2: Build frontend**
```bash
cd frontend && npm run build
```
Expected: 0 errors

**Step 3: Run tests if available**
```bash
dotnet test
```

---

## Summary of Changes

| Area | Change | File |
|------|--------|------|
| Generator form | Block when no controller, zero prev readings, disable form | AddReading.jsx |
| Generator backend | Reject reading creation without controller | ReadingService.cs |
| Site form | Block when no meter, validate site+meter IDs, show IDs | SubStationReading.jsx |
| Site backend | Reject reading creation without meter, require subStationId | SubStationReadingService.cs |
| Previous readings | Existing fallback already correct (DailyReading → Controller → null) | No change needed |
