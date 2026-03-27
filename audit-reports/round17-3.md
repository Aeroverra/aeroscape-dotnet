# AUDIT ROUND 17 - CRITICAL SWEEP
**Files: LoginFrames.cs + GameFrames.cs + OutgoingFrame.cs**
**Status: 16 rounds of fixes applied**

## CRITICAL BUGS FOUND

### 1. **LoginFrames.cs - SetString Method Unsafe with Large Strings**
**Location:** Lines 60-69
```csharp
private static void WriteSetString(FrameWriter w, string text, int interfaceId, int childId)
{
    int sSize = text.Length + 5; // string length + newline terminator + 2 words
    w.CreateFrame(179);
    w.WriteByte(sSize / 256);
    w.WriteByte(sSize % 256);
```
**Issue:** If string is too long (>65530 chars), the size calculation overflows when writing to 2 bytes. This will cause packet corruption.
**Fix Required:** Add length validation: `if (text.Length > 65530) throw new ArgumentException("String too long for protocol");`

### 2. **LoginFrames.cs - Race Condition in WriteMapRegion Recovery**
**Location:** Lines 276-295
```csharp
if (keys == null)
{
    // Fixed: Prevent infinite recursion by checking if this is already a recovery attempt
    if (isRecoveryAttempt)
    {
        // If recovery also failed, send empty region frame to prevent crash
        w.WriteDWord(0); // Default XTEA key
```
**Issue:** The recovery logic still has a flaw. If multiple threads call WriteMapRegion concurrently and both hit missing keys, they could both trigger recovery attempts simultaneously, causing state corruption since Player coordinates are mutated without synchronization.
**Fix Required:** Add thread synchronization around the coordinate mutation block.

### 3. **GameFrames.cs - Buffer Overflow in EncryptPlayerChat**
**Location:** Lines 769-828
```csharp
// Fixed: Add bounds check to prevent buffer overflow
if (byteIndex >= output.Length) break;
output[byteIndex] = (byte)(carry = carry | UnsignedRightShift(encoded, shift));
```
**Issue:** The bounds check is incomplete. It only checks `byteIndex` but not `lastByte`. If `lastByte >= output.Length` and the code continues, it will still overflow on subsequent iterations.
**Fix Required:** Also validate `lastByte < output.Length` before the nested if statements.

### 4. **GameFrames.cs - Integer Overflow in SetMapRegion**
**Location:** Lines 703-705
```csharp
int region = yCalc + (xCalc << 8);
```
**Issue:** If `xCalc` is large enough (>16777215), the shift operation will overflow into negative values. While unlikely in practice, this is a latent bug.
**Fix Required:** Add bounds validation or use unchecked arithmetic with explicit masking.

### 5. **OutgoingFrame.cs - Bit Access State Corruption**
**Location:** Lines 119-124
```csharp
public void WriteByte(int val)
{
    // Fixed: Ensure we're not in bit access mode before writing bytes
    if (_bitPosition > _offset * 8)
    {
        throw new InvalidOperationException("Cannot write bytes while in bit access mode. Call FinishBitAccess() first.");
    }
```
**Issue:** The check `_bitPosition > _offset * 8` is incorrect. It should be `_bitPosition != _offset * 8`. If `_bitPosition` is less than `_offset * 8` (which shouldn't happen but could due to bugs), it won't be caught.
**Fix Required:** Change to `!=` for exact state validation.

### 6. **GameFrames.cs - Unvalidated Array Access in SetItems**
**Location:** Lines 677-696
```csharp
for (int i = 0; i < itemArray.Length; i++)
{
    if (itemAmt[i] > 254)
```
**Issue:** No validation that `itemAmt.Length >= itemArray.Length`. If `itemAmt` is shorter, this will throw IndexOutOfRangeException.
**Fix Required:** Add validation: `if (itemAmt.Length < itemArray.Length) throw new ArgumentException("itemAmt array too short");`

### 7. **LoginFrames.cs - Missing Null Check in WriteWeaponTab**
**Location:** Line 188
```csharp
string weapon = items.GetItemName(p.Equipment[CombatConstants.SlotWeapon]);
```
**Issue:** If `GetItemName` returns null (which it might for invalid item IDs), subsequent string operations will throw NullReferenceException.
**Fix Required:** Add null coalescing: `string weapon = items.GetItemName(...) ?? "";`

### 8. **GameFrames.cs - Potential Null Reference in SendClanChat**
**Location:** Lines 42-44
```csharp
byte[] bytes = new byte[message.Length + 1];
bytes[0] = (byte)message.Length;
EncryptPlayerChat(bytes, 0, 1, message.Length, message);
```
**Issue:** No null check on `message` parameter. If null is passed, this will crash.
**Fix Required:** Add guard: `if (message == null) throw new ArgumentNullException(nameof(message));`

### 9. **OutgoingFrame.cs - Memory Leak Risk in EnsureCapacity**
**Location:** Lines 258-265
```csharp
private void EnsureCapacity(int additional)
{
    if (_offset + additional > _buf.Length)
    {
        int newSize = Math.Max(_buf.Length * 2, _offset + additional + 256);
        Array.Resize(ref _buf, newSize);
    }
}
```
**Issue:** No upper bound on buffer growth. A malicious or buggy caller could cause unbounded memory allocation.
**Fix Required:** Add max size check: `if (newSize > 10_000_000) throw new InvalidOperationException("Frame too large");`

### 10. **GameFrames.cs - Division by Zero in WriteBits**
**Location:** Lines 224-225 (OutgoingFrame.cs)
```csharp
public void WriteBits(int numBits, int value)
{
    int bytePos = _bitPosition >> 3;
```
**Issue:** If `numBits` is 0, the loop logic could behave unexpectedly. While not a crash, it's undefined behavior.
**Fix Required:** Add guard: `if (numBits <= 0) return;`

## Summary
Found 10 critical bugs across all three files. Each represents a potential crash, security vulnerability, or protocol corruption issue. All require immediate fixes before production deployment.