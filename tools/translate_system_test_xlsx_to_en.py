# -*- coding: utf-8 -*-
"""Translate System Test.xlsx (Club Fund sheet) from Vietnamese to English in place."""
from __future__ import annotations

import re
from pathlib import Path

import openpyxl

REPO = Path(__file__).resolve().parents[1]
XLSX = REPO / "System Test.xlsx"

# Vietnamese Latin extended + common chars
_VIET = re.compile(r"[\u00C0-\u024F\u1E00-\u1EFF]")

# Full string -> English (built from sheet content)
TRANSLATIONS: dict[str, str] = {
    "Workflow": "Workflow",
    "Club Fund": "Club Fund",
    "Test requirement": "Test requirement",
    "Number of TCs": "Number of TCs",
    "Testing Round": "Testing Round",
    "Passed": "Passed",
    "Failed": "Failed",
    "Pending": "Pending",
    "N/A": "N/A",
    "Round 1": "Round 1",
    "Round 2": "Round 2",
    "System test luồng Club Fund: tạo quỹ, đóng góp, PayOS payment/webhook/return, duyệt quỹ, xem lịch sử và quyền (capabilities).": (
        "System test for the Club Fund flow: create fund, contribute, PayOS payment/webhook/return, "
        "approve/reject fund, view history and permissions (capabilities)."
    ),
    "Quỹ tồn tại.": "The fund exists.",
    "Có quỹ PENDING.": "A fund in PENDING state exists.",
    "Có quỹ quá hạn.": "An expired fund exists.",
    "User đăng nhập.": "User is logged in.",
    "Có return endpoint.": "Return endpoint is available.",
    "Có endpoint webhook.": "Webhook endpoint is available.",
    "User có quyền xem quỹ.": "User has permission to view funds.",
    "User là member thường.": "User is a regular member.",
    "User đủ quyền tạo quỹ.": "User has permission to create a fund.",
    "Có secret webhook đúng.": "Correct webhook secret is configured.",
    "Quỹ có rule min amount.": "Fund has minimum amount rule.",
    "Có contribution PENDING.": "A PENDING contribution exists.",
    "Quỹ có lịch sử giao dịch.": "Fund has transaction history.",
    "Bị chặn quyền.\nKhông tạo quỹ.": "Access denied.\nFund is not created.",
    "Trả NotFound.\nKhông có thay đổi.": "Returns NotFound.\nNo changes.",
    "Không tạo đóng góp khi Amount = 0": "Cannot create contribution when Amount = 0",
    "User đăng nhập nhưng không thuộc CLB.": "User is logged in but is not a club member.",
    "Có quỹ PENDING.\nUser role top manager.": "A fund in PENDING exists.\nUser has top manager role.",
    "Không duyệt được khi quỹ không tồn tại": "Cannot approve when the fund does not exist",
    "Webhook PayOS: body rỗng -> BadRequest": "PayOS webhook: empty body -> BadRequest",
    "Hiển thị lỗi validation.\nKhông tạo quỹ.": "Validation error is shown.\nFund is not created.",
    "Không tạo đóng góp khi Amount < minimum": "Cannot create contribution when Amount < minimum",
    "Không tạo đóng góp khi quỹ không tồn tại": "Cannot create contribution when the fund does not exist",
    "Không tạo được quỹ khi initial amount âm": "Cannot create fund when initial amount is negative",
    "1. Đăng nhập.\n2. Mở fundId không tồn tại.": "1. Log in.\n2. Open a non-existent fundId.",
    "Có contribution PENDING + webhook hợp lệ.": "PENDING contribution exists + valid webhook.",
    "Top manager reject quỹ PENDING thành công": "Top manager successfully rejects a PENDING fund",
    "Không tạo được quỹ khi tên quỹ trống/space": "Cannot create fund when fund name is empty/whitespace",
    "Top manager approve quỹ PENDING thành công": "Top manager successfully approves a PENDING fund",
    "Không duyệt được khi không phải top manager": "Cannot approve when user is not top manager",
    "Không tạo được quỹ khi member inactive/LEFT": "Cannot create fund when member is inactive/LEFT",
    "Webhook PayOS: code != 00 -> không finalize": "PayOS webhook: code != 00 -> not finalized",
    "Bị chặn quyền (403).\nKhông đổi trạng thái quỹ.": "Forbidden (403).\nFund status is unchanged.",
    "Trả NotFound hoặc thông báo quỹ không tồn tại.": "Returns NotFound or a message that the fund does not exist.",
    "User ACTIVE nhưng role không đủ quyền tạo quỹ.": "User is ACTIVE but role cannot create funds.",
    "API trả 400 BadRequest.\nKhông cập nhật dữ liệu.": "API returns 400 BadRequest.\nData is not updated.",
    "Hiển thị lỗi validation.\nKhông tạo contribution.": "Validation error is shown.\nContribution is not created.",
    "Xem chi tiết quỹ: fundId tồn tại -> hiển thị đúng": "View fund details: existing fundId -> correct display",
    "Không tạo đóng góp khi quỹ đã hết hạn (nếu có hạn)": "Cannot create contribution when fund has expired (if expiry applies)",
    "Xem chi tiết quỹ: fundId không tồn tại -> NotFound": "View fund details: non-existent fundId -> NotFound",
    "Không tạo đóng góp khi quỹ chưa được duyệt (PENDING)": "Cannot create contribution when fund is not approved (PENDING)",
    "Trả NotFound hoặc thông báo giao dịch không tồn tại.": "Returns NotFound or a message that the transaction does not exist.",
    "Không tạo được quỹ khi user không phải member của CLB": "Cannot create fund when user is not a club member",
    "1. Đăng nhập.\n2. Chọn quỹ đã quá hạn.\n3. Thử đóng góp.": "1. Log in.\n2. Select an expired fund.\n3. Attempt to contribute.",
    "1. Gửi webhook với signature sai.\n2. Quan sát response.": "1. Send webhook with invalid signature.\n2. Observe response.",
    "Hiển thị lỗi validation số tiền.\nKhông tạo contribution.": "Amount validation error is shown.\nContribution is not created.",
    "User thuộc CLB nhưng trạng thái membership không ACTIVE.": "User belongs to the club but membership is not ACTIVE.",
    "User thuộc CLB và có role Vice Manager level 2 (ACTIVE).": "User belongs to the club with Vice Manager role level 2 (ACTIVE).",
    "1. Đăng nhập top manager.\n2. Chọn quỹ PENDING.\n3. Reject.": "1. Log in as top manager.\n2. Select a PENDING fund.\n3. Reject.",
    "1. Đăng nhập.\n2. Chọn quỹ.\n3. Nhập Amount = 0.\n4. Submit.": "1. Log in.\n2. Select a fund.\n3. Enter Amount = 0.\n4. Submit.",
    "API trả 400 BadRequest.\nKhông cập nhật contribution/fund.": "API returns 400 BadRequest.\nContribution/fund is not updated.",
    "Capabilities: member thường không có quyền approve/create": "Capabilities: regular member has no approve/create rights",
    "History paging: page/pageSize hợp lệ -> trả đúng số lượng": "History paging: valid page/pageSize -> correct number of items returned",
    "PayOS Return: truy cập return khi orderCode không tồn tại": "PayOS Return: hit return URL when orderCode does not exist",
    "Đóng góp thành công và nhận link PayOS khi số tiền hợp lệ": "Contribution succeeds and PayOS link is returned for a valid amount",
    "1. Gửi request webhook với body rỗng.\n2. Quan sát response.": "1. Send webhook request with empty body.\n2. Observe response.",
    "1. Gửi webhook với code khác '00'.\n2. Quan sát transaction.": "1. Send webhook with code other than '00'.\n2. Observe transaction.",
    "1. Đăng nhập.\n2. Chọn quỹ status = PENDING.\n3. Thử đóng góp.": "1. Log in.\n2. Select a fund with status = PENDING.\n3. Attempt to contribute.",
    "Hiển thị đúng tên quỹ, số dư, status, ngày hết hạn (nếu có).": "Shows correct fund name, balance, status, expiry date (if any).",
    "Quỹ chuyển status = APPROVED.\nHiển thị thông báo thành công.": "Fund status becomes APPROVED.\nSuccess message is shown.",
    "1. Đăng nhập vice manager/member.\n2. Thử approve quỹ PENDING.": "1. Log in as vice manager/member.\n2. Attempt to approve a PENDING fund.",
    "Webhook PayOS: success -> contribution PAID và quỹ tăng số dư": "PayOS webhook: success -> contribution PAID and fund balance increases",
    "1. Đăng nhập top manager.\n2. Approve với fundId không tồn tại.": "1. Log in as top manager.\n2. Approve with non-existent fundId.",
    "1. Đăng nhập.\n2. Nhập Amount nhỏ hơn mức tối thiểu.\n3. Submit.": "1. Log in.\n2. Enter amount below minimum.\n3. Submit.",
    "Tạo quỹ thành công khi user là Vice Manager (status = PENDING)": "Fund created successfully when user is Vice Manager (status = PENDING)",
    "1. Đăng nhập user role member thường.\n2. Thử tạo quỹ.\n3. Submit.": "1. Log in as a regular member.\n2. Attempt to create a fund.\n3. Submit.",
    "Bị chặn quyền (403 hoặc thông báo không có quyền).\nKhông tạo quỹ.": "Access denied (403 or no-permission message).\nFund is not created.",
    "Không tạo được quỹ khi user là member thường (không phải quản lý)": "Cannot create fund when user is a regular member (not management)",
    "1. Đăng nhập member thường.\n2. Mở capabilities.\n3. Quan sát flags.": "1. Log in as regular member.\n2. Open capabilities.\n3. Observe flags.",
    "1. Đăng nhập user đủ quyền.\n2. Nhập InitialAmount = -1.\n3. Submit.": "1. Log in as authorized user.\n2. Enter InitialAmount = -1.\n3. Submit.",
    "Hiển thị lỗi validation rõ ràng cho trường tên quỹ.\nKhông tạo quỹ.": "Clear validation error for fund name.\nFund is not created.",
    "Bị chặn thao tác.\nThông báo quỹ đã hết hạn.\nKhông tạo contribution.": "Action blocked.\nMessage that fund has expired.\nContribution is not created.",
    "Không tạo được quỹ khi ExpiresAt là ngày trong quá khứ (nếu có hạn)": "Cannot create fund when ExpiresAt is in the past (if expiry is used)",
    "1. Gọi return URL với orderCode không tồn tại.\n2. Quan sát response.": "1. Call return URL with non-existent orderCode.\n2. Observe response.",
    "1. Đăng nhập user đủ quyền.\n2. Chọn hạn đóng quỹ < hôm nay.\n3. Submit.": "1. Log in as authorized user.\n2. Set fund closing date before today.\n3. Submit.",
    "1. Đăng nhập user top manager.\n2. Mở danh sách quỹ PENDING.\n3. Approve.": "1. Log in as top manager.\n2. Open PENDING funds list.\n3. Approve.",
    "1. Đăng nhập.\n2. Truy cập đóng góp với fundId không tồn tại.\n3. Submit.": "1. Log in.\n2. Open contribute flow with non-existent fundId.\n3. Submit.",
    "Quỹ tồn tại, status = APPROVED, chưa hết hạn.\nPayOS sandbox configured.": "Fund exists, status = APPROVED, not expired.\nPayOS sandbox configured.",
    "Tạo quỹ thành công khi user là Manager cấp cao nhất (status = APPROVED)": "Fund created successfully when user is top-level Manager (status = APPROVED)",
    "Bị chặn thao tác.\nThông báo quỹ chưa được duyệt.\nKhông tạo contribution.": "Action blocked.\nMessage that fund is not approved.\nContribution is not created.",
    "Flags thể hiện không được tạo/duyệt quỹ.\nChỉ được xem/đóng góp theo rule.": "Flags show no create/approve fund.\nView/contribute only per rules.",
    "1. Đăng nhập member của CLB.\n2. Mở quỹ theo fundId.\n3. Quan sát thông tin.": "1. Log in as club member.\n2. Open fund by fundId.\n3. Observe details.",
    "User thuộc CLB và có role Manager level 1.\nCLB tồn tại và user đang ACTIVE.": "User belongs to the club with Manager role level 1.\nClub exists and user is ACTIVE.",
    "Danh sách trả về đúng số lượng.\nCó tổng số trang/tổng bản ghi nếu API hỗ trợ.": "List returns correct count.\nTotal pages/total records if API supports.",
    "Quỹ chuyển status = REJECTED (hoặc trạng thái tương ứng).\nHiển thị thông báo.": "Fund status becomes REJECTED (or equivalent).\nMessage is shown.",
    "1. Đăng nhập user không thuộc CLB.\n2. Truy cập tạo quỹ theo clubId.\n3. Submit.": "1. Log in as user not in the club.\n2. Open create fund for clubId.\n3. Submit.",
    "Hiển thị lỗi NotFound hoặc thông báo quỹ không tồn tại.\nKhông tạo contribution.": "NotFound or fund-not-found message.\nContribution is not created.",
    "1. Đăng nhập.\n2. Mở lịch sử quỹ.\n3. Set page=1,pageSize=10.\n4. Quan sát kết quả.": "1. Log in.\n2. Open fund history.\n3. Set page=1, pageSize=10.\n4. Observe results.",
    "1. Đăng nhập user từng là member nhưng status != ACTIVE.\n2. Thử tạo quỹ.\n3. Submit.": "1. Log in as former member with status != ACTIVE.\n2. Attempt to create fund.\n3. Submit.",
    "Tạo quỹ thành công.\nQuỹ được tạo với status = PENDING.\nHiển thị thông báo chờ duyệt.": "Fund created successfully.\nFund is created with status = PENDING.\nPending-approval message is shown.",
    "1. Đăng nhập bằng user là Vice Manager level 2.\n2. Tạo quỹ với dữ liệu hợp lệ.\n3. Submit.": "1. Log in as Vice Manager level 2.\n2. Create fund with valid data.\n3. Submit.",
    "1. Tạo contribution PENDING.\n2. Gửi webhook success cho đúng orderCode.\n3. Reload quỹ và history.": "1. Create PENDING contribution.\n2. Send success webhook for correct orderCode.\n3. Reload fund and history.",
    "1. Đăng nhập user đủ quyền.\n2. Vào tạo quỹ.\n3. Để trống FundName hoặc nhập toàn space.\n4. Submit.": "1. Log in as authorized user.\n2. Go to create fund.\n3. Leave FundName empty or whitespace.\n4. Submit.",
    "API trả 200/OK (hoặc theo spec) nhưng transaction không được đánh dấu paid.\nKhông cộng tiền vào quỹ.": "API returns 200/OK (per spec) but transaction is not marked paid.\nFund balance is not increased.",
    "Contribution chuyển sang PAID/SUCCESS.\nQuỹ tăng CurrentBalance đúng số tiền.\nHistory có bản ghi mới.": "Contribution becomes PAID/SUCCESS.\nFund CurrentBalance increases by correct amount.\nHistory has a new record.",
    "1. Đăng nhập user là member của CLB.\n2. Mở chi tiết quỹ status = APPROVED.\n3. Nhập Amount >= min.\n4. Submit đóng góp.": "1. Log in as club member.\n2. Open APPROVED fund details.\n3. Enter Amount >= min.\n4. Submit contribution.",
    "1. Đăng nhập bằng user là Manager level 1 của CLB.\n2. Vào màn hình tạo quỹ.\n3. Nhập dữ liệu hợp lệ (tên, số tiền, hạn nếu có).\n4. Submit.": "1. Log in as club Manager level 1.\n2. Open create fund screen.\n3. Enter valid data (name, amount, expiry if any).\n4. Submit.",
    "Tạo quỹ thành công.\nQuỹ được tạo với status = APPROVED.\nThông báo thành công hiển thị, redirect về chi tiết quỹ hoặc danh sách quỹ.": "Fund created successfully.\nFund is created with status = APPROVED.\nSuccess message shown; redirect to fund details or fund list.",
    "Tạo contribution thành công.\nNhận được thông tin thanh toán PayOS (paymentUrl/qr/orderCode).\nTrạng thái đóng góp = PENDING (chờ thanh toán).": "Contribution created successfully.\nPayOS payment info received (paymentUrl/qr/orderCode).\nContribution status = PENDING (awaiting payment).",
}


def translate_cell(value: object) -> object:
    if not isinstance(value, str):
        return value
    s = value
    if s in TRANSLATIONS:
        return TRANSLATIONS[s]
    if not _VIET.search(s):
        return value
    # Partial: try line-by-line
    lines = s.split("\n")
    out_lines = []
    changed = False
    for line in lines:
        if line in TRANSLATIONS:
            out_lines.append(TRANSLATIONS[line])
            changed = True
        else:
            out_lines.append(line)
    if changed:
        return "\n".join(out_lines)
    return value


def main() -> None:
    if not XLSX.is_file():
        raise SystemExit(f"Missing: {XLSX}")
    wb = openpyxl.load_workbook(XLSX)
    for ws in wb.worksheets:
        for row in ws.iter_rows():
            for cell in row:
                if cell.value is None:
                    continue
                new_val = translate_cell(cell.value)
                if new_val != cell.value:
                    cell.value = new_val
    wb.save(XLSX)
    print(f"Saved: {XLSX}")


if __name__ == "__main__":
    main()
