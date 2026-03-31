# Cocorra API — Mobile Developer Reference
**Version:** 1.0 | **Build:** ✅ 0 Errors, 0 Warnings

---

## Table of Contents
1. [Global Configuration](#1-global-configuration)
2. [Auth Module](#2-auth-module)
3. [Profile Module](#3-profile-module)
4. [Friends Module](#4-friends-module)
5. [Notifications Module](#5-notifications-module)
6. [Chat Module — REST](#6-chat-module--rest)
7. [Rooms Module — REST](#7-rooms-module--rest)
8. [Admin Module](#8-admin-module)
9. [Roles Module](#9-roles-module)
10. [SignalR — ChatHub](#10-signalr--chathub)
11. [SignalR — RoomHub](#11-signalr--roomhub)
12. [Enum Reference](#12-enum-reference)

---

## 1. Global Configuration

### Base URL
```
https://<your-server>/
```
All REST routes are relative to this base. Routes that use `Router.cs` constants resolve to:
```
Api/V1/<Module>/<Action>
```
Routes using `[Route("api/[controller]")]` resolve to:
```
api/<ControllerName>/<Action>
```

### Authentication
All protected endpoints require a JWT Bearer token:
```
Authorization: Bearer <token>
```
The token is obtained from the Login endpoint and expires in **24 hours**.

### Global Pagination
Endpoints that return lists accept these query parameters:

| Parameter | Type | Default | Max | Notes |
|-----------|------|---------|-----|-------|
| `pageNumber` | int | 1 | — | Minimum 1 |
| `pageSize` | int | varies | varies | Server-enforced cap |

### Standard Response Envelope
Every response (success and error) wraps data in this structure:
```json
{
  "data": <payload or null>,
  "succeeded": true,
  "message": "Human-readable message or null",
  "statusCode": 200,
  "meta": null
}
```
On **error**, `succeeded` is `false`, `data` is `null`, and `message` contains the reason.

**Paginated responses** include `meta`:
```json
{
  "data": [...],
  "succeeded": true,
  "message": null,
  "statusCode": 200,
  "meta": {
    "totalCount": 150,
    "currentPage": 2,
    "pageSize": 20
  }
}
```

### Rate Limiting
- **100 requests per minute** per IP address.
- Exceeding the limit returns HTTP **429 Too Many Requests**.

### HTTP Status Codes
| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 400 | Bad request / validation failed / business rule violated |
| 401 | Missing or invalid token |
| 403 | Authenticated but not authorized (wrong role) |
| 404 | Resource not found |
| 429 | Rate limit exceeded |
| 500 | Internal server error |

---

## 2. Auth Module

**Base path:** `Api/V1/Authentication`

---

### 2.1 Register
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Authentication/Register` |
| **Auth** | ❌ Public |
| **Content-Type** | `multipart/form-data` |

**Request Fields:**

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `firstName` | string | ✅ | Max 100 chars |
| `lastName` | string | ✅ | Max 100 chars |
| `age` | int | ✅ | — |
| `email` | string | ✅ | Valid email, 5–100 chars |
| `password` | string | ✅ | Min 8 chars, must include upper, lower, digit, special char |
| `confirmPassword` | string | ✅ | Must match `password` |
| `voiceVerification` | file | ✅ | Audio file (.m4a, .aac, .wav, etc.) |
| `profilePicture` | file | ✅ | Image file |

**Success Response (201):**
```json
{
  "data": "Registration successful! Check Your Email.",
  "succeeded": true,
  "statusCode": 201
}
```

> **Note:** User starts with `Pending` status. An OTP is emailed for verification. The account cannot login until admin approves and status becomes `Active`.

---

### 2.2 Confirm Email (Verify OTP)
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `Api/V1/Authentication/ConfirmEmail` |
| **Auth** | ❌ Public |

**Query Parameters:**

| Param | Type | Required |
|-------|------|----------|
| `email` | string | ✅ |
| `otpCode` | string | ✅ |

**Success Response (200):**
```json
{
  "data": "Email confirmed successfully.",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 2.3 Resend OTP
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Authentication/ResendOtp` |
| **Auth** | ❌ Public |
| **Content-Type** | `application/json` |

**Request Body:** A raw JSON string (the email address):
```json
"user@example.com"
```

**Success Response (200):**
```json
{
  "data": "OTP resent successfully.",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 2.4 Login
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Authentication/Login` |
| **Auth** | ❌ Public |
| **Content-Type** | `application/json` |

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecureP@ss1"
}
```

**Success Response (200):**
```json
{
  "data": {
    "message": null,
    "isAuthenticated": true,
    "username": "user@example.com",
    "email": "user@example.com",
    "roles": ["User"],
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "expiresOn": "2026-04-01T21:00:00Z"
  },
  "succeeded": true,
  "statusCode": 200
}
```

**Status-based rejection messages:**

| Status | Error Message |
|--------|---------------|
| `Pending` | "Your account is still pending approval. We usually respond within 24 hours." |
| `Rejected` | "Your account has been rejected." |
| `Banned` | "Your account has been banned." |
| `ReRecord` | "Your voice verification was not accepted. Please re-record and resubmit." |
| Email not confirmed | "Please confirm your email before logging in." |

> Only `Active` status allows login.

---

### 2.5 Forgot Password
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Authentication/ForgotPassword` |
| **Auth** | ❌ Public |
| **Content-Type** | `application/json` |

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Response (200):** Always succeeds (no user enumeration):
```json
{
  "data": "If your email is registered, you will receive a password reset code shortly.",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 2.6 Reset Password (with OTP)
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Authentication/ResetPassword` |
| **Auth** | ❌ Public |
| **Content-Type** | `application/json` |

**Request Body:**
```json
{
  "email": "user@example.com",
  "otpCode": "123456",
  "newPassword": "NewSecureP@ss1"
}
```

| Field | Constraint |
|-------|-----------|
| `email` | Valid email |
| `otpCode` | 6-digit code from email |
| `newPassword` | Min 6 chars |

**Success Response (200):**
```json
{
  "data": "Password has been reset successfully.",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 2.7 Submit MBTI
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Authentication/SubmitMbti` |
| **Auth** | ✅ Authenticated |
| **Content-Type** | `application/json` |

**Request Body:**
```json
{
  "mBTI": "INTJ"
}
```

**Success Response (200):**
```json
{
  "data": "MBTI updated successfully.",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 2.8 Update FCM Token
| | |
|---|---|
| **Method** | `PUT` |
| **Route** | `Api/V1/Authentication/UpdateFcmToken` |
| **Auth** | ✅ Authenticated |
| **Content-Type** | `application/json` |

**Request Body:** Raw JSON string (the FCM device token):
```json
"fcm-device-token-string-here"
```

**Success Response (200):**
```json
{
  "data": "FCM Token updated successfully.",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 2.9 Re-Record Voice *(for ReRecord status users)*
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Authentication/ReRecordVoice` |
| **Auth** | ✅ Authenticated |
| **Content-Type** | `multipart/form-data` |

> Only works if the authenticated user's status is `ReRecord`. Returns 400 otherwise.

**Form Fields:**

| Field | Type | Required |
|-------|------|----------|
| `voiceFile` | file | ✅ |

**Success Response (200):**
```json
{
  "data": "Voice re-recorded successfully. Your account is now pending review.",
  "succeeded": true,
  "statusCode": 200
}
```

After this call, the user's status reverts to `Pending` for admin re-review.

---

### 2.10 Update Password *(for authenticated users)*
| | |
|---|---|
| **Method** | `PUT` |
| **Route** | `Api/V1/Authentication/UpdatePassword` |
| **Auth** | ✅ Authenticated |
| **Content-Type** | `application/json` |

**Request Body:**
```json
{
  "currentPassword": "OldP@ssword1",
  "newPassword": "NewP@ssword2"
}
```

| Field | Constraint |
|-------|-----------|
| `currentPassword` | Required |
| `newPassword` | Required, min 8 chars |

**Success Response (200):**
```json
{
  "data": "Password updated successfully.",
  "succeeded": true,
  "statusCode": 200
}
```

---

## 3. Profile Module

**Base path:** `api/Profile`
**Auth:** ✅ All endpoints require authentication.

---

### 3.1 Get My Profile
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `api/Profile/me` |

**Success Response (200) — `MyProfileDto`:**
```json
{
  "data": {
    "id": "user-guid",
    "firstName": "Ahmed",
    "lastName": "Hassan",
    "email": "ahmed@example.com",
    "profilePicturePath": "uploads/images/abc123.jpg",
    "bio": "Passionate speaker and learner.",
    "age": 28,
    "mBTI": "INTJ"
  },
  "succeeded": true,
  "statusCode": 200
}
```

> `profilePicturePath` and `bio` and `mBTI` may be `null` if not set.

---

### 3.2 Get Public Profile
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `api/Profile/{targetUserId}` |

**Path Params:** `targetUserId` — Guid of the user to view.

**Success Response (200) — `PublicProfileDto`:**
```json
{
  "data": {
    "userId": "target-user-guid",
    "fullName": "Sara Ali",
    "profilePicturePath": "uploads/images/xyz.jpg",
    "bio": "Coffee lover.",
    "mBTI": "ENFP",
    "friendshipStatus": 0,
    "isFriend": false
  },
  "succeeded": true,
  "statusCode": 200
}
```

`friendshipStatus` values: `0` = Pending, `1` = Accepted, `2` = Rejected, `null` = no relationship.

---

### 3.3 Update Profile
| | |
|---|---|
| **Method** | `PUT` |
| **Route** | `api/Profile/update` |
| **Content-Type** | `application/json` |

**Request Body:**
```json
{
  "firstName": "Ahmed",
  "lastName": "Hassan",
  "bio": "My updated bio here.",
  "age": 29
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `firstName` | string | ✅ | Max 50 chars |
| `lastName` | string | ✅ | Max 50 chars |
| `bio` | string? | ❌ | Max 500 chars |
| `age` | int | ✅ | 18–120 |

**Success Response (200):**
```json
{
  "data": "Profile updated successfully.",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 3.4 Upload Profile Picture
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `api/Profile/upload-picture` |
| **Content-Type** | `multipart/form-data` |

**Form Fields:**

| Field | Type | Required |
|-------|------|----------|
| `file` | image file | ✅ |

**Success Response (200):**
```json
{
  "data": "uploads/images/new-picture-path.jpg",
  "succeeded": true,
  "statusCode": 200
}
```

---

## 4. Friends Module

**Base path:** `api/Friends`
**Auth:** ✅ All endpoints require authentication.

---

### 4.1 Search User by ID
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `api/Friends/search/{targetId}` |

**Path Params:** `targetId` — Guid of the user to look up.

**Success Response (200) — `UserSearchDto`:**
```json
{
  "data": {
    "id": "target-user-guid",
    "fullName": "Sara Ali",
    "friendshipStatus": "None"
  },
  "succeeded": true,
  "statusCode": 200
}
```

`friendshipStatus` values: `"None"`, `"Pending"`, `"Accepted"`, `"Rejected"`.

---

### 4.2 Send Friend Request
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `api/Friends/send-request` |
| **Content-Type** | `application/json` |

**Request Body:**
```json
{
  "targetUserId": "target-user-guid"
}
```

**Success Response (200):**
```json
{
  "data": "Friend request sent successfully.",
  "succeeded": true,
  "statusCode": 200
}
```

> If a previous rejected request exists, it is recycled and re-sent instead of creating a new row.

---

### 4.3 Respond to Friend Request
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `api/Friends/respond-request/{senderId}` |

**Path Params:** `senderId` — Guid of the user who sent the request.

**Query Params:**

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `accept` | bool | ✅ | `true` = accept, `false` = reject |

**Success Response (200):**
```json
{
  "data": "Friend request accepted." ,
  "succeeded": true,
  "statusCode": 200
}
```

---

### 4.4 Remove Friend or Cancel Request
| | |
|---|---|
| **Method** | `DELETE` |
| **Route** | `api/Friends/remove/{targetId}` |

**Path Params:** `targetId` — Guid of the friend or pending requester.

Works for:
- Removing an accepted friend.
- Cancelling a pending outgoing request you sent.

**Success Response (200):**
```json
{
  "data": "Removed successfully.",
  "succeeded": true,
  "statusCode": 200
}
```

---

## 5. Notifications Module

**Base path:** `api/Notifications`
**Auth:** ✅ All endpoints require authentication.

---

### 5.1 Get My Notifications
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `api/Notifications/my-notifications` |

**Query Params:**

| Param | Default | Max |
|-------|---------|-----|
| `pageNumber` | 1 | — |
| `pageSize` | 20 | 100 |

**Success Response (200) — `NotificationResponseDto[]`:**
```json
{
  "data": [
    {
      "id": "notification-guid",
      "title": "New Friend Request",
      "message": "Ahmed Hassan sent you a friend request.",
      "type": "FriendRequest",
      "referenceId": "friend-request-guid-or-null",
      "isRead": false,
      "createdAt": "2026-03-31T18:00:00Z"
    }
  ],
  "succeeded": true,
  "statusCode": 200
}
```

**`type` values:** `System`, `RoomReminder`, `FriendRequest`, `FriendAccept`

`referenceId` points to the relevant entity (room ID for reminders, request ID for friend notifications). May be `null` for System notifications.

---

### 5.2 Mark Single Notification as Read
| | |
|---|---|
| **Method** | `PUT` |
| **Route** | `api/Notifications/read-notification/{notificationId}` |

**Path Params:** `notificationId` — Guid of the notification.

**Success Response (200):**
```json
{
  "data": "Notification marked as read.",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 5.3 Mark All Notifications as Read
| | |
|---|---|
| **Method** | `PUT` |
| **Route** | `api/Notifications/mark-all-read` |

**Success Response (200):**
```json
{
  "data": "All notifications marked as read.",
  "succeeded": true,
  "statusCode": 200
}
```

---

## 6. Chat Module — REST

**Base path:** `api/Chat`
**Auth:** ✅ All endpoints require authentication.

---

### 6.1 Get Chat Friends List (Inbox)
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `api/Chat/friends-list` |

**Query Params:**

| Param | Default | Max |
|-------|---------|-----|
| `pageNumber` | 1 | — |
| `pageSize` | 20 | 100 |

**Success Response (200) — `ChatFriendDto[]`:**
```json
{
  "data": [
    {
      "friendId": "friend-guid",
      "fullName": "Sara Ali",
      "profilePicturePath": "uploads/images/sara.jpg",
      "lastMessage": "See you tomorrow!",
      "lastMessageDate": "2026-03-31T20:45:00Z",
      "unreadCount": 3
    }
  ],
  "succeeded": true,
  "statusCode": 200
}
```

> `profilePicturePath` is `""` (empty string) if no picture — never `null`.
> `lastMessage` is `""` if no messages yet.
> `lastMessageDate` is `null` if no messages exist — handle null before parsing.
> Results sorted by most recent conversation first.

---

### 6.2 Get Chat History
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `api/Chat/history/{friendId}` |

**Path Params:** `friendId` — Guid of the friend.

**Query Params:**

| Param | Default | Max | Notes |
|-------|---------|-----|-------|
| `pageNumber` | 1 | — | Page 1 = newest messages |
| `pageSize` | 50 | 100 | |

**Success Response (200) — `MessageDto[]`:**
```json
{
  "data": [
    {
      "id": "msg-guid",
      "senderId": "your-user-guid",
      "receiverId": "friend-guid",
      "content": "Hello!",
      "isRead": true,
      "createdAt": "2026-03-31T19:00:00Z"
    },
    {
      "id": "msg-guid-2",
      "senderId": "friend-guid",
      "receiverId": "your-user-guid",
      "content": "Hi there!",
      "isRead": false,
      "createdAt": "2026-03-31T19:01:00Z"
    }
  ],
  "succeeded": true,
  "statusCode": 200
}
```

> **Read receipts:** If `senderId == currentUserId && isRead == true` → show double ticks (✓✓). If `isRead == false` → single tick (✓).
> **Infinite scroll:** Increment `pageNumber` as user scrolls up to load older messages.

---

### 6.3 Mark Messages as Read
| | |
|---|---|
| **Method** | `PUT` |
| **Route** | `api/Chat/mark-read/{friendId}` |

**Path Params:** `friendId` — Guid of the friend whose messages to mark read.

Marks **all unread messages from that friend** as read in a single database operation.

**Success Response (200):**
```json
{
  "data": "Messages marked as read.",
  "succeeded": true,
  "statusCode": 200
}
```

> Call this when the user **opens** a conversation screen, not on every message render.

---

## 7. Rooms Module — REST

**Base path:** Mixed routing (see each endpoint).
**Auth:** ✅ All endpoints require authentication.

---

### 7.1 Create Room
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Room/Create` |
| **Content-Type** | `application/json` |

**Request Body:**
```json
{
  "roomTitle": "English Speaking Practice",
  "description": "Daily practice for B2+ speakers",
  "scheduledStartDate": null,
  "isPrivate": false,
  "totalCapacity": 50,
  "stageCapacity": 5,
  "defaultSpeakerDurationMinutes": 5,
  "selectionMode": 0
}
```

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| `roomTitle` | string | ✅ | — | Max 100 chars |
| `description` | string? | ❌ | null | Optional |
| `scheduledStartDate` | DateTime? | ❌ | null | Future date → Scheduled; null or past → Live immediately |
| `isPrivate` | bool | ❌ | false | Private rooms require host approval to join |
| `totalCapacity` | int | ❌ | 50 | Max listeners |
| `stageCapacity` | int | ❌ | 5 | Max concurrent speakers |
| `defaultSpeakerDurationMinutes` | int | ❌ | 5 | Speaking time per speaker |
| `selectionMode` | int | ❌ | 0 | `0` = Manual_CoachDecision |

**Success Response (200):**
```json
{
  "data": "new-room-guid",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 7.2 Get Rooms Feed
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `Api/V1/Room/Feed` |

**Query Params:**

| Param | Default | Max |
|-------|---------|-----|
| `pageNumber` | 1 | — |
| `pageSize` | 20 | **50** (hard cap) |

**Success Response (200) — `RoomSummaryDto[]`:**
```json
{
  "data": [
    {
      "id": "room-guid",
      "roomTitle": "English Practice",
      "description": "Daily session",
      "status": 1,
      "scheduledStartDate": null,
      "listenersCount": 24,
      "isReminderSetByMe": false,
      "hostName": "Ahmed Hassan"
    },
    {
      "id": "room-guid-2",
      "roomTitle": "Book Club",
      "description": "Discussing Clean Code",
      "status": 0,
      "scheduledStartDate": "2026-04-02T15:00:00Z",
      "listenersCount": 6,
      "isReminderSetByMe": true,
      "hostName": "Sara Ali"
    }
  ],
  "succeeded": true,
  "statusCode": 200
}
```

**`status` values:** `0` = Scheduled, `1` = Live, `2` = Ended, `3` = Cancelled

> For **Live** rooms: `listenersCount` = active participants. For **Scheduled** rooms: `listenersCount` = reminder count. Use `isReminderSetByMe` to control the reminder bell icon state.

---

### 7.3 Join Room
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Room/Join` |

> ⚠️ The roomId is embedded in the route — check the Join route definition in Router.cs. Currently: `Api/V1/Room/Join` (no `{roomId}` in path). Confirm with your router config.

**Success Response — public room (200):**
```json
{
  "data": true,
  "succeeded": true,
  "message": "Joined successfully.",
  "statusCode": 200
}
```

**Success Response — private room, pending approval (200):**
```json
{
  "data": false,
  "succeeded": true,
  "message": "Request sent, waiting for approval.",
  "statusCode": 200
}
```

> After a successful REST join, you must then connect to **RoomHub** via WebSocket and invoke `JoinRoom`. See §11.

---

### 7.4 Approve User (Host Only — Private Rooms)
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Room/Approve` |

**Query Params:**

| Param | Type | Required |
|-------|------|----------|
| `roomId` | Guid | ✅ |
| `userId` | Guid | ✅ |

**Success Response (200):**
```json
{
  "data": "User approved.",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 7.5 Get Room State
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `Api/V1/Room/{roomId}/State` |

**Path Params:** `roomId` — Guid of the room.

**Success Response (200) — `RoomStateDto`:**
```json
{
  "data": {
    "roomId": "room-guid",
    "roomTitle": "English Practice",
    "hostId": "host-guid",
    "totalCapacity": 50,
    "stageCapacity": 5,
    "participants": [
      {
        "userId": "user-guid-1",
        "name": "Ahmed Hassan",
        "isOnStage": true,
        "isMuted": false,
        "isHandRaised": false,
        "joinedAt": "2026-03-31T18:00:00Z"
      },
      {
        "userId": "user-guid-2",
        "name": "Sara Ali",
        "isOnStage": false,
        "isMuted": true,
        "isHandRaised": true,
        "joinedAt": "2026-03-31T18:05:00Z"
      }
    ]
  },
  "succeeded": true,
  "statusCode": 200
}
```

> Call this immediately after joining to **hydrate the initial room state**. After that, rely on SignalR events for real-time updates.

---

### 7.6 Toggle Reminder
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Room/{roomId}/toggle-reminder` |

**Path Params:** `roomId` — Guid of the scheduled room.

**Success Response (200):**
```json
{
  "data": "Reminder set successfully.",
  "succeeded": true,
  "statusCode": 200
}
```
Or `"Reminder removed."` if it was already set.

---

### 7.7 Start Scheduled Room (Host Only)
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `api/Rooms/{roomId}/start` |

**Path Params:** `roomId` — Guid of the scheduled room.

Changes room status from `Scheduled` → `Live` and notifies all reminder subscribers.

**Success Response (200):**
```json
{
  "data": "Room is now live and notifications have been sent!",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 7.8 End Room (Host Only)
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `api/Rooms/{roomId}/end` |

**Path Params:** `roomId` — Guid of the live room.

Atomically: sets status to `Ended`, finalizes all participants' spoken time, and cleans up all active members.

**Success Response (200):**
```json
{
  "data": "Room has been ended successfully.",
  "succeeded": true,
  "statusCode": 200
}
```

---

## 8. Admin Module

**Base path:** `Api/V1/Admin`
**Auth:** 🔴 `Admin` role required for ALL endpoints.

---

### 8.1 Get All Users (with Search & Pagination)
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `Api/V1/Admin/Users` |

**Query Params:**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `search` | string? | null | Searches email, firstName, lastName |
| `page` | int | 1 | Page number |
| `pageSize` | int | 10 | Items per page |

**Success Response (200) — `UserDto[]`:**
```json
{
  "data": [
    {
      "id": "user-guid-string",
      "fullName": "Ahmed Hassan",
      "email": "ahmed@example.com",
      "age": 28,
      "mBTI": "INTJ",
      "status": "Pending",
      "voicePath": "uploads/voices/ahmed.m4a"
    }
  ],
  "succeeded": true,
  "statusCode": 200,
  "meta": {
    "totalCount": 150,
    "currentPage": 1,
    "pageSize": 10
  }
}
```

---

### 8.2 Get User by ID
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `Api/V1/Admin/User/{id}` |

**Path Params:** `id` — Guid of the user.

**Success Response (200) — Single `UserDto` (same structure as above).**

---

### 8.3 Change User Status
| | |
|---|---|
| **Method** | `PUT` |
| **Route** | `Api/V1/Admin/User/ChangeStatus/{id}` |
| **Content-Type** | `application/json` |

**Path Params:** `id` — Guid of the user.

**Request Body:**
```json
{
  "newStatus": 1
}
```

`newStatus` integer values:

| Value | Status | Effect |
|-------|--------|--------|
| 0 | `Pending` | No lockout change |
| 1 | `Active` | Removes lockout, deletes voice file |
| 2 | `Rejected` | Deletes voice file |
| 3 | `Banned` | Permanent lockout, deletes voice file |
| 4 | `ReRecord` | Deletes voice file, user must re-upload |

**Success Response (200):**
```json
{
  "data": "User status changed from Pending to Active",
  "succeeded": true,
  "statusCode": 200
}
```

---

### 8.4 Dashboard Stats
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `Api/V1/Admin/Dashboard/Stats` |

**Success Response (200) — `DashboardStatsDto`:**
```json
{
  "data": {
    "totalUsers": 500,
    "activeUsers": 320,
    "pendingUsers": 80,
    "bannedUsers": 15,
    "rejectedUsers": 60,
    "reRecordUsers": 25
  },
  "succeeded": true,
  "statusCode": 200
}
```

---

## 9. Roles Module

**Base path:** `Api/V1/Roles`
**Auth:** 🔴 `Admin` role required for ALL endpoints.

---

### 9.1 Get All Roles
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `Api/V1/Roles/List` |

**Success Response (200) — `RoleDto[]`:**
```json
{
  "data": [
    { "id": "role-guid", "name": "Admin" },
    { "id": "role-guid-2", "name": "User" }
  ],
  "succeeded": true,
  "statusCode": 200
}
```

---

### 9.2 Get Role by ID
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `Api/V1/Roles/{id}` |

---

### 9.3 Manage User Roles
| | |
|---|---|
| **Method** | `POST` |
| **Route** | `Api/V1/Roles/ManageUser` |
| **Content-Type** | `application/json` |

**Request Body:**
```json
{
  "userId": "user-guid",
  "roles": ["Admin", "User"]
}
```

---

### 9.4 Get Users in Role
| | |
|---|---|
| **Method** | `GET` |
| **Route** | `Api/V1/Roles/Users/{roleName}` |

**Path Params:** `roleName` — e.g., `"Admin"` or `"User"`.

---

## 10. SignalR — ChatHub

### Connection
```
WebSocket URL: wss://<your-server>/chatHub?access_token=<JWT>
```

**Authentication:** Pass the JWT as the `access_token` query parameter. Do not use HTTP headers — SignalR WebSockets cannot carry them.

```dart
// Flutter example
final connection = HubConnectionBuilder()
    .withUrl('https://your-server/chatHub',
        HttpConnectionOptions(
          accessTokenFactory: () async => await getToken(),
        ))
    .withAutomaticReconnect()
    .build();
await connection.start();
```

---

### Methods to Invoke (Client → Server)

#### `SendMessage`
Send a message to another user.

| Parameter | Type | Description |
|-----------|------|-------------|
| `receiverIdString` | string | Recipient's UserId as a GUID string |
| `content` | string | Message text (cannot be empty/whitespace) |

```
connection.invoke("SendMessage", "receiver-guid-string", "Hello!")
```

**Possible errors (thrown as HubException):**
- `"Invalid User IDs."` — malformed GUID
- `"Message cannot be empty."` — empty or whitespace content
- `"You are not friends."` or other service-level errors

---

### Events to Listen To (Server → Client)

#### `ReceiveMessage`
Fires on the **recipient's** client when someone sends them a message.

```json
{
  "id": "msg-guid",
  "senderId": "sender-guid",
  "receiverId": "your-guid",
  "content": "Hello!",
  "isRead": false,
  "createdAt": "2026-03-31T19:00:00Z"
}
```

**Action:** If the sender's chat screen is open → append message and call `PUT api/Chat/mark-read/{senderId}`. Otherwise → increment unread badge on inbox.

---

#### `MessageSent`
Fires on the **sender's** client confirming their message was saved.

```json
{
  "id": "msg-guid",
  "senderId": "your-guid",
  "receiverId": "receiver-guid",
  "content": "Hello!",
  "isRead": false,
  "createdAt": "2026-03-31T19:00:00Z"
}
```

**Action:** Replace the local "sending…" placeholder with this confirmed message. Use `id` for deduplication.

---

## 11. SignalR — RoomHub

### Connection
```
WebSocket URL: wss://<your-server>/roomHub?access_token=<JWT>
```

Authentication is identical to ChatHub.

---

### Two-Step Join Flow (Critical)
```
Step 1: POST Api/V1/Room/Join   (registers you in the database)
Step 2: Connect to /roomHub WebSocket
Step 3: invoke("JoinRoom", roomId)  (adds you to the SignalR group)
Step 4: GET Api/V1/Room/{roomId}/State  (hydrate initial UI state)
```

Skipping Step 1 causes `JoinRoom` to throw `"You are not a member of this room."`.

---

### Methods to Invoke (Client → Server)

#### `JoinRoom`
Add yourself to the room's real-time channel.

| Parameter | Type |
|-----------|------|
| `roomId` | string (GUID) |

```
connection.invoke("JoinRoom", "room-guid-string")
```

**Errors:**
- `"Room is not live yet or has ended."` — room status is not Live
- `"You are not a member of this room."` — skipped REST join
- `"Your request is still pending approval from the host."` — private room, not yet approved
- `"You are not allowed to join this room."` — kicked or rejected

---

#### `LeaveRoom`
Gracefully exit the room.

| Parameter | Type |
|-----------|------|
| `roomId` | string (GUID) |

```
connection.invoke("LeaveRoom", "room-guid-string")
```

> Always call this **before** disconnecting. If the connection drops unexpectedly, the server auto-cleans up via `OnDisconnectedAsync`.

---

#### `RaiseHand`
Request to be moved to the stage (listeners only).

| Parameter | Type |
|-----------|------|
| `roomId` | string (GUID) |

```
connection.invoke("RaiseHand", "room-guid-string")
```

No-op if already on stage. Broadcasts `HandRaised` to all participants.

---

#### `ApproveToStage` *(Host only)*
Promote a listener to the speaker stage.

| Parameter | Type |
|-----------|------|
| `roomId` | string (GUID) |
| `targetUserId` | string (GUID) |

```
connection.invoke("ApproveToStage", "room-guid", "target-user-guid")
```

**Errors:**
- `"Only the host can approve speakers to the stage."`
- `"Stage is full. Someone must leave the stage first."`

---

#### `MoveToAudience` *(Host only)*
Demote a speaker back to the audience. Finalizes their spoken time and mutes them.

| Parameter | Type |
|-----------|------|
| `roomId` | string (GUID) |
| `targetUserId` | string (GUID) |

```
connection.invoke("MoveToAudience", "room-guid", "target-user-guid")
```

**Error:** `"Only the host can demote speakers."`

---

#### `ToggleMic`
Mute or unmute yourself (speakers only). Tracks spoken time server-side.

| Parameter | Type | Description |
|-----------|------|-------------|
| `roomId` | string (GUID) | |
| `muteStatus` | bool | `true` = mute, `false` = unmute |

```
connection.invoke("ToggleMic", "room-guid", false)  // unmute
connection.invoke("ToggleMic", "room-guid", true)   // mute
```

**Error:** `"Your time is up! The host needs to grant you more time."` — time allowance exhausted.

> No-op for audience members or if room doesn't exist.

---

#### `GrantExtraTime` *(Host only)*
Give a speaker additional speaking time.

| Parameter | Type | Constraints |
|-----------|------|-------------|
| `roomId` | string (GUID) | |
| `targetUserId` | string (GUID) | |
| `minutes` | int | **1–30 minutes** |

```
connection.invoke("GrantExtraTime", "room-guid", "speaker-guid", 5)
```

**Error:** `"Extra time must be between 1 and 30 minutes."`

---

#### `KickUser` *(Host only)*
Permanently remove a user from the room.

| Parameter | Type |
|-----------|------|
| `roomId` | string (GUID) |
| `targetUserId` | string (GUID) |

```
connection.invoke("KickUser", "room-guid", "target-guid")
```

**Errors:**
- `"The host cannot kick themselves."`
- `"Only the host can kick users."`

Kicked users receive the `UserKicked` event and cannot re-join this room.

---

#### `EndRoom` *(Host only)*
Terminate the room entirely.

| Parameter | Type |
|-----------|------|
| `roomId` | string (GUID) |

```
connection.invoke("EndRoom", "room-guid")
```

All participants receive `RoomEnded` and are cleaned up server-side.

---

### Events to Listen To (Server → Client)

#### `UserJoined`
Someone connected to the room.
```json
{
  "userId": "user-guid",
  "name": "Ahmed Hassan",
  "isOnStage": false
}
```
**Action:** Add to participants list. If `isOnStage` is true, place in stage section.

---

#### `UserLeft`
Someone left (gracefully or via disconnect).
```json
{
  "userId": "user-guid"
}
```
**Action:** Remove from participants list.

---

#### `HandRaised`
A listener requests to speak.
```json
{
  "userId": "user-guid",
  "name": "Sara Ali"
}
```
**Action (Host):** Show hand-raise indicator. Display an "Approve" button.

---

#### `StageUpdated`
A participant moved to/from the stage.
```json
{
  "userId": "user-guid",
  "isOnStage": true,
  "name": "Sara Ali"
}
```
**Action:** Move user between stage and audience sections based on `isOnStage`.

---

#### `MicStatusChanged`
Mic state changed for a participant.
```json
{
  "userId": "user-guid",
  "isMuted": false,
  "name": "Sara",
  "remainingSeconds": 243.0
}
```
**Action:** Toggle mic icon. If `userId == currentUser`, update or start the countdown timer using `remainingSeconds`. Display as `MM:SS`.

> `remainingSeconds` is `0` when time is exhausted. `MoveToAudience` also triggers this event with `isMuted: true` and no `remainingSeconds`.

---

#### `ExtraTimeGranted`
Host gave more speaking time to a participant.
```json
{
  "userId": "speaker-guid",
  "addedMinutes": 5,
  "name": "Sara"
}
```
**Action:** Show toast notification. Update countdown timer for the speaker.

---

#### `UserKicked`
A user was removed by the host.
```json
{
  "userId": "kicked-user-guid",
  "name": "Ahmed Hassan"
}
```
**Action:**
- If `userId == currentUserId` → navigate back to rooms feed immediately. Show alert: "You have been removed from this room."
- Otherwise → remove the user from the participants list.

---

#### `RoomEnded`
The host ended the room.
```json
{
  "roomId": "room-guid",
  "message": "The host has ended this room."
}
```
**Action:** Navigate ALL participants back to the rooms feed. Show a dialog or toast with the message.

---

### Reconnection Strategy
Use `.withAutomaticReconnect()`. After reconnect:
1. Re-invoke `JoinRoom(roomId)` to rejoin the SignalR group.
2. Call `GET Api/V1/Room/{roomId}/State` to re-hydrate the participant list (events missed during disconnect window).

---

## 12. Enum Reference

### UserStatus
| Int | Name | Description |
|-----|------|-------------|
| 0 | `Pending` | Awaiting admin review |
| 1 | `Active` | Can login and use the app |
| 2 | `Rejected` | Account rejected by admin |
| 3 | `Banned` | Permanently banned |
| 4 | `ReRecord` | Must re-upload voice verification |

### RoomStatus
| Int | Name |
|-----|------|
| 0 | `Scheduled` |
| 1 | `Live` |
| 2 | `Ended` |
| 3 | `Cancelled` |

### FriendRequestStatus
| Int | Name |
|-----|------|
| 0 | `Pending` |
| 1 | `Accepted` |
| 2 | `Rejected` |

### NotificationType
| Int | Name | Use |
|-----|------|-----|
| 0 | `System` | Admin broadcast messages |
| 1 | `RoomReminder` | Scheduled room is about to start |
| 2 | `FriendRequest` | Someone sent you a friend request |
| 3 | `FriendAccept` | Someone accepted your friend request |

### RoomSelectionMode
| Int | Name |
|-----|------|
| 0 | `Manual_CoachDecision` |

---

*Generated: 2026-04-01 | Cocorra API v1 | Build: ✅ 0 Errors, 0 Warnings*
