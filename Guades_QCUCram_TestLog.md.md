**# QCU-CRAM — Step 36 Test Log**

**Tester: \_\_\_\_\_\_\_\_\_\_\_\_  Date: \_\_\_\_\_\_\_\_\_\_\_\_**

**Start from a clean DB (Schema.sql + Seed.sql). Default accounts: admin / admin123.**

**Use Dev Tools → clock buttons for time-based tests. Click "Reset clock" when done.**



**| # | Scenario | Expected | Pass | Notes |**

**|---|---|---|---|---|**

**| T1 | Register with duplicate student number | Friendly rejection | ☐ | |**

**| T2 | Login with wrong password | Generic "Invalid credentials" | ☐ | |**

**| T3 | Student opens /Guades\_QCUCram\_Admin/Guades\_QCUCram\_Queue.aspx | Redirected to login | ☐ | |**

**| T4 | Book for today | Rejected (one-day-advance rule) | ☐ | |**

**| T5 | Book 2+ days ahead | Rejected | ☐ | |**

**| T6 | Book with 4 members, then 11 members | Rejected (5–10) | ☐ | |**

**| T7 | Count says 6, only 5 names filled | Rejected | ☐ | |**

**| T8 | Same student books 2 slots on same date | 2nd rejected | ☐ | |**

**| T9 | Two students book same room/slot | 2nd rejected (conflict) | ☐ | |**

**| T10 | Double-click Submit / two browsers at once | Only one reservation created | ☐ | |**

**| T11 | Valid booking | Pending + history row + "received" notification; grid shows Pending | ☐ | |**

**| T12 | Book a blocked slot via URL/field tampering | Server still rejects | ☐ | |**

**| T13 | Admin approves | Approved + notification; grid Taken | ☐ | |**

**| T14 | Decline without reason, then with reason | Blocked, then Declined; slot reopens | ☐ | |**

**| T15 | Reassign to taken slot, then open slot | Blocked, then Reassigned; note correct | ☐ | |**

**| T16 | Student cancels before start | Cancelled; slot reopens; no strike | ☐ | |**

**| T17 | Check-in before start / after grace | Refused both | ☐ | |**

**| T18 | Check-in within window | CheckedIn; CheckedInAt set | ☐ | |**

**| T19 | Clock past slot end after check-in | Sweep → Completed | ☐ | |**

**| T20 | Approved, no check-in, clock > start+15 min | Forfeited; NoShowCount=1; notification | ☐ | |**

**| T21 | Pending never reviewed, clock > start | Declined (expired) | ☐ | |**

**| T22 | 3 forfeitures | Restricted + lift date + notification | ☐ | |**

**| T23 | Restricted student tries to book | Blocked with banner | ☐ | |**

**| T24 | Clock past RestrictionDays; student logs in | Auto-lifted; strikes reset | ☐ | |**

**| T25 | Admin "Lift now" | Restriction removed; student notified | ☐ | |**

**| T26 | Set GraceMinutes = 5 in Settings | Sweep uses 5 | ☐ | |**

**| T27 | Disable a room with future bookings | Not offered for new bookings; old ones still display | ☐ | |**

**| T28 | Print page (PrintBooking) | Single clean page, correct members | ☐ | |**

**| T29 | Analytics vs. Guades\_QCUCram\_Analytics.sql counts | Match | ☐ | |**

**| T30 | Session timeout / Back after logout | Forced re-login | ☐ | |**

