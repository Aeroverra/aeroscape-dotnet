# Audit Report - Round 17 - PacketDecoders.cs Comprehensive Sweep

## Overview
This is the CRITICAL FINAL audit after 16 rounds and 246+ bug fixes. Every line of code has been examined with extreme scrutiny.

## Bugs Found

### 1. PublicChatDecoder - Color Parameter Always 0
**Location:** Lines 162-172
**Severity:** High - Feature broken
**Issue:** The decoder returns `new PublicChatMessage(text, effects, 0)` with a hardcoded 0 for the color parameter. The Java decoder (ChatMessages.java) reads a proper color value.
**Fix:** Should read the color byte from the payload.

### 2. ObjectOption2Decoder - Protocol Mismatch  
**Location:** Lines 433-447
**Severity:** High - Functionality broken
**Issue:** The decoder reads 6 bytes as object data (objectId, x, y) but returns `ObjectOption2Message` which expects player interaction data according to the comment about Java being "misnamed/repurposed for player interactions". This is a fundamental protocol mismatch.
**Fix:** Either:
- Create a proper object-based message type for this opcode
- Or read the correct player interaction data if that's what the protocol actually sends

### 3. NoOpDecoder - Misleading Return
**Location:** Lines 688-693
**Severity:** Medium - Confusing behavior
**Issue:** Returns `null` instead of an actual `IdleMessage` instance, despite the MessageType indicating it should return IdleMessage.
**Fix:** Should either:
- Return `new IdleMessage()` to match the declared type
- Or change MessageType to a proper NoOpMessage type

### 4. TradeAcceptDecoder - Insufficient Validation
**Location:** Lines 853-882
**Severity:** Medium - Potential crashes
**Issue:** While some validation exists, it doesn't handle:
- Integer overflow when calculating partnerId (if raw is very large)
- Negative partner IDs if raw value causes underflow
- The magic numbers (33024, 256) have no explanation
**Fix:** Add overflow checks and document the protocol constants properly

### 5. ItemOnItemDecoder - Inconsistent Documentation
**Location:** Lines 491-508
**Severity:** Low - Documentation issue
**Issue:** Comment says "RS 508 ItemOnItem: the Java handler only reads the first 4 bytes" but then the decoder reads all 16 bytes. This is confusing - either the comment is wrong or we're reading unnecessary data.
**Fix:** Clarify whether we need to read all 16 bytes or just the first 4.

### 6. ActionButtonsDecoder - Magic Number 65535
**Location:** Lines 217-234
**Severity:** Low - Code clarity
**Issue:** Uses magic number 65535 without explanation. This appears to be checking for -1 in unsigned form but should be documented or use a named constant.
**Fix:** Add comment or use const like `SLOT_NONE = 65535`.

### 7. EquipItemDecoder - Misleading Variable Name
**Location:** Lines 247-257
**Severity:** Low - Code clarity
**Issue:** Reads a value into variable named "junk" but doesn't document WHY it's junk or what the 4 bytes represent in the protocol.
**Fix:** Add comment explaining what these 4 bytes are in the protocol and why they're ignored.

### 8. DialogueContinueDecoder - Wasted Reads
**Location:** Lines 820-827
**Severity:** Low - Performance
**Issue:** Comment says "6 bytes — interface hash + slot, consumed but not used by legacy" but the decoder doesn't actually consume these bytes. This could cause buffer position issues if the protocol expects them to be consumed.
**Fix:** Should read and discard the 6 bytes to properly consume the packet.

## Summary
After 16 rounds of fixes, 8 bugs remain:
- 2 High severity (broken functionality)
- 2 Medium severity (potential crashes/confusion)
- 4 Low severity (clarity/performance)

The most critical issues are PublicChatDecoder always returning color=0 and ObjectOption2Decoder's fundamental protocol mismatch.