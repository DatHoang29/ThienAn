---
name: plan-writing
description: Lập kế hoạch nhiều bước thành tệp prompt theo documentation-standards §1. Dùng khi chia việc, sắp thứ tự phụ thuộc, đặt cách kiểm cho từng bước, hoặc ghi chỗ cần sửa sau review.
---

# Kế hoạch là tệp prompt

Luật gốc: [documentation-standards_thienan.md](../../rules/documentation-standards_thienan.md) §1.

- Tệp `DocBusinessThienAn/<DựÁn>/<PhânHệ>/Prompt/<task-slug>-prompt.md`. Có cả BE và FE thì tách `<task-slug>-be-prompt.md`, `<task-slug>-fe-prompt.md` (Rule 19.98).
- Thêm dòng vào `Prompt/README.md` theo nếp sẵn có, trạng thái ⏳ Chưa thực thi. Không xóa prompt khi xong (Rule 19.34).
- Không tạo `{task-slug}.md` ở gốc repo, `PLAN.md` hay kế hoạch theo ngày (Rule 19.52).

## Khung

```markdown
# Prompt — <Tên việc>

> Tệp: `DocBusinessThienAn/<DựÁn>/<PhânHệ>/Prompt/<task-slug>-prompt.md`
> Nguồn: <transcript, review hoặc yêu cầu người dùng>

## Mục tiêu
<Một câu.>

## Các bước
1. <Tệp, vị trí, cũ → mới> → Kiểm: <lệnh test hoặc điều thấy được>
2. ...

## Việc cuối — Cập nhật lại tài liệu gốc
<MasterPlan và tài liệu liên quan cần cập nhật.>
```

## Nguyên tắc
- 5-10 bước, mỗi bước một kết quả kiểm được. Ghi bước nào chờ bước nào; việc độc lập chạy song song được (Rule 19.70).
- Cách kiểm là lệnh thật: `dotnet test tests/BE/test.csproj -f net10.0 --filter "FullyQualifiedName~<LớpTest>"`, `pnpm test:e2e` trong `tests/FE/`. Không có bước build FE.
- Căn cứ chỉ lấy mã working tree và DB local.
