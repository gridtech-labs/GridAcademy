# Exam Content API Examples

## Create Exam
`POST /api/exams`

```json
{
  "name": "SSC CGL",
  "category": "SSC",
  "level": "Graduate",
  "isActive": true
}
```

## Create Notification
`POST /api/notifications`

```json
{
  "examId": "0f649a0e-1849-44e7-9ad8-bae6db9f73b6",
  "title": "SSC CGL Notification 2026 Released",
  "contentHtml": "<p>SSC released official notification ...</p>",
  "importantDates": "[{\"label\":\"Apply Start\",\"date\":\"2026-04-10\"}]",
  "notificationType": 1,
  "sourceUrl": "https://ssc.gov.in/notice/abc",
  "canonicalUrl": "https://gridacademy.in/notifications/ssc-cgl-notification-2026",
  "metaTitle": "SSC CGL 2026 Notification",
  "metaDescription": "Important SSC CGL dates, eligibility and links.",
  "status": 1
}
```

## Get Notification by Slug
`GET /api/notifications/ssc-cgl-notification-2026-notification-2026`

```json
{
  "success": true,
  "data": {
    "id": "9f8d6ec8-3f17-48a4-9fdd-ed68e0f28ec1",
    "examId": "0f649a0e-1849-44e7-9ad8-bae6db9f73b6",
    "examName": "SSC CGL",
    "title": "SSC CGL Notification 2026 Released",
    "slug": "ssc-cgl-notification-2026-notification-2026",
    "summary": "ssc released official notification ...",
    "notificationType": 1,
    "sourceUrl": "https://ssc.gov.in/notice/abc",
    "canonicalUrl": "https://gridacademy.in/notifications/ssc-cgl-notification-2026",
    "status": 1
  }
}
```
