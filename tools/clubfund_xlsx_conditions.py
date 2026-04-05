# -*- coding: utf-8 -*-
"""
Derive Unit Test.xlsx Condition / Confirm (Return) marks from ClubFund test C# sources.

Precondition: short English in D10 only (D9 left blank). No O on rows 9–10 per UTCID.

Layout (1-based rows):
  Row 9, col D — empty
  Row 10, col D — specific precondition (English)
  Row 11 — B11 parameter label
  Row 12–13 — value rows + O per UTCID when matched
  Row 15–16 — Confirm T/F per UTCID
"""
from __future__ import annotations

import re
from typing import Literal

Receiver = Literal["_controller.", "_service."]

ROW_PRE = 9
ROW_DB = 10
ROW_PARAM_LABEL = 11
ROW_VAL_LO = 12
ROW_VAL_HI = 13
ROW_RET_OK = 15
ROW_RET_ERR = 16
UTC_START = 5

ConditionKind = str


def slice_test_method_body(cs: str, test_name: str) -> str | None:
    pat = rf"public\s+async\s+Task\s+{re.escape(test_name)}\s*\([^)]*\)\s*{{"
    m = re.search(pat, cs)
    if not m:
        return None
    start = m.end() - 1
    depth = 0
    i = start
    while i < len(cs):
        if cs[i] == "{":
            depth += 1
        elif cs[i] == "}":
            depth -= 1
            if depth == 0:
                return cs[start + 1 : i]
        i += 1
    return None


def _balanced_call_args(body: str, receiver: str, method: str) -> str | None:
    m = re.search(rf"{re.escape(receiver)}{re.escape(method)}\s*\(", body)
    if not m:
        return None
    open_i = m.end() - 1
    depth = 0
    i = open_i
    while i < len(body):
        if body[i] == "(":
            depth += 1
        elif body[i] == ")":
            depth -= 1
            if depth == 0:
                return body[open_i + 1 : i]
        i += 1
    return None


def extract_route_id(
    body: str,
    receiver: Receiver,
    method: str,
    kind: ConditionKind,
) -> int | None:
    if kind in ("none", "webhook", "get_my_clubs_auth"):
        return None
    if kind == "approve_fund_id":
        m = re.search(r"_controller\.ApproveFund\s*\([\s\S]*?FundId\s*=\s*(\d+)", body)
        return int(m.group(1)) if m else None
    if kind == "order_first":
        m = re.search(r"_controller\.GetPayOsContributionReturn\s*\(\s*(\d+)", body)
        return int(m.group(1)) if m else None
    if kind == "club_first":
        m = re.search(rf"{re.escape(receiver)}{re.escape(method)}\s*\(\s*(\d+)", body)
        return int(m.group(1)) if m else None
    if kind == "fund_first":
        m = re.search(rf"{re.escape(receiver)}{re.escape(method)}\s*\(\s*(\d+)", body)
        return int(m.group(1)) if m else None
    if kind == "service_dto_club":
        inner = _balanced_call_args(body, receiver, method)
        if not inner:
            return None
        m = re.search(r"ClubId\s*=\s*(\d+)", inner)
        return int(m.group(1)) if m else None
    if kind == "service_dto_fund":
        inner = _balanced_call_args(body, receiver, method)
        if not inner:
            return None
        m = re.search(r"FundId\s*=\s*(\d+)", inner)
        return int(m.group(1)) if m else None
    if kind == "service_first_int":
        m = re.search(rf"{re.escape(receiver)}{re.escape(method)}\s*\(\s*(\d+)", body)
        return int(m.group(1)) if m else None
    if kind == "service_int_after_guid":
        m = re.search(rf"{re.escape(receiver)}{re.escape(method)}\s*\(\s*[^,]+,\s*(\d+)", body)
        return int(m.group(1)) if m else None
    if kind == "service_fund_id_only":
        m = re.search(rf"{re.escape(receiver)}{re.escape(method)}\s*\(\s*(\d+)", body)
        return int(m.group(1)) if m else None
    if kind == "service_order_code":
        m = re.search(rf"{re.escape(receiver)}{re.escape(method)}\s*\(\s*[^,]+,\s*(\d+)", body)
        return int(m.group(1)) if m else None
    if kind == "service_approve_fund":
        m = re.search(rf"{re.escape(receiver)}{re.escape(method)}\s*\([\s\S]*?FundId\s*=\s*(\d+)", body)
        return int(m.group(1)) if m else None
    if kind == "service_try_pending":
        m = re.search(rf"{re.escape(receiver)}{re.escape(method)}\s*\(\s*[^,]+,\s*(\d+)", body)
        return int(m.group(1)) if m else None
    if kind == "service_process_payos":
        m = re.search(rf"{re.escape(receiver)}{re.escape(method)}\s*\(\s*(\d+)", body)
        return int(m.group(1)) if m else None
    return None


CONTROLLER_KIND: dict[str, ConditionKind] = {
    "C.ClubFund.CreateFund": "club_first",
    "C.ClubFund.GetMyClubs": "get_my_clubs_auth",
    "C.ClubFund.GetFundCapabilities": "club_first",
    "C.ClubFund.GetReportSummary": "club_first",
    "C.ClubFund.GetFundCategories": "club_first",
    "C.ClubFund.GetFundTrans": "club_first",
    "C.ClubFund.GetFund": "fund_first",
    "C.ClubFund.GetFundsByClub": "club_first",
    "C.ClubFund.GetMyFunds": "club_first",
    "C.ClubFund.Contribute": "club_first",
    "C.ClubFund.GetContrPayStatus": "club_first",
    "C.ClubFund.GetPayOsReturn": "order_first",
    "C.ClubFund.SimulatePayOsDev": "club_first",
    "C.ClubFund.ApproveFund": "approve_fund_id",
    "C.ClubFund.PayOSWebhook": "webhook",
    "C.ClubFund.GetHistory": "club_first",
    "C.ClubFund.GetFundLocation": "fund_first",
}

SERVICE_KIND: dict[str, ConditionKind] = {
    "S.ClubFund.CreateFundAsync": "service_dto_club",
    "S.ClubFund.CreateContribAsync": "service_dto_fund",
    "S.ClubFund.GetContrPayStatus": "service_int_after_guid",
    "S.ClubFund.GetPayStatusByOrd": "service_order_code",
    "S.ClubFund.GetFundByIdAsync": "service_fund_id_only",
    "S.ClubFund.GetFundsByClubPaged": "service_first_int",
    "S.ClubFund.GetMyFundsPaged": "service_first_int",
    "S.ClubFund.GetFundHistoryPaged": "service_first_int",
    "S.ClubFund.ApproveFundAsync": "service_approve_fund",
    "S.ClubFund.ProcessPayOSSuccess": "service_process_payos",
    "S.ClubFund.TryCompletePending": "service_try_pending",
    "S.ClubFund.GetFundCapabilities": "service_int_after_guid",
    "S.ClubFund.ReportSummaryAsync": "service_first_int",
    "S.ClubFund.ClubFundTransPaged": "service_first_int",
}

CONTROLLER_PARAM_LABEL: dict[str, str] = {
    "C.ClubFund.GetFund": "FundId (route)",
    "C.ClubFund.GetFundLocation": "FundId (route)",
    "C.ClubFund.GetPayOsReturn": "orderCode",
    "C.ClubFund.ApproveFund": "FundId (body)",
    "C.ClubFund.PayOSWebhook": "—",
}

SERVICE_PARAM_LABEL: dict[str, str] = {
    "S.ClubFund.CreateContribAsync": "FundId (request)",
    "S.ClubFund.GetFundByIdAsync": "FundId",
    "S.ClubFund.GetPayStatusByOrd": "orderCode",
    "S.ClubFund.ProcessPayOSSuccess": "orderCode",
    "S.ClubFund.TryCompletePending": "clubId (route context)",
}

def precondition_d10_english(kind: ConditionKind) -> str:
    """One short line: business-specific preconditions only (no environment boilerplate)."""
    if kind == "webhook":
        return "PayOS webhook HTTP request with JSON body."
    if kind == "none":
        return "Authenticated user; no club route parameter."
    if kind == "get_my_clubs_auth":
        return "HttpContext.User: empty claims vs authenticated user."
    if kind in ("fund_first", "service_fund_id_only"):
        return "Fund row present or missing per mock."
    if kind in ("order_first", "service_order_code", "service_process_payos"):
        return "Contribution / order row per mock."
    if kind in ("approve_fund_id", "service_approve_fund"):
        return "Fund + approver role per mock."
    if kind == "service_dto_club":
        return "User + club fund rules per mock."
    if kind == "service_dto_fund":
        return "Fund + PayOS mocks as per test."
    if kind == "service_try_pending":
        return "User, club, fund IDs per mock."
    return "Club access + fund/member mocks per test."


def apply_business_condition_matrix(
    ws,
    *,
    tests: list[str],
    cs_text: str,
    sheet_title: str,
    csharp_method: str,
    controller: bool,
    scenario_type_fn,
) -> None:
    """D9 blank; D10 short English precondition; fill 11–13, 15–16 (no O on rows 9–10 per UTCID)."""
    receiver: Receiver = "_controller." if controller else "_service."
    kind = (CONTROLLER_KIND if controller else SERVICE_KIND).get(sheet_title, "club_first")

    ws.cell(ROW_PRE, 4).value = None
    ws.cell(ROW_DB, 4).value = precondition_d10_english(kind)

    label_map = CONTROLLER_PARAM_LABEL if controller else SERVICE_PARAM_LABEL
    if sheet_title in label_map:
        ws.cell(ROW_PARAM_LABEL, 2).value = label_map[sheet_title]
    elif kind in ("fund_first", "service_fund_id_only"):
        ws.cell(ROW_PARAM_LABEL, 2).value = "FundId"
    elif kind == "webhook":
        ws.cell(ROW_PARAM_LABEL, 2).value = "—"
    elif kind == "get_my_clubs_auth":
        ws.cell(ROW_PARAM_LABEL, 2).value = "User / auth context"
    else:
        ws.cell(ROW_PARAM_LABEL, 2).value = "ClubId (route)"

    route_ids: list[int | None] = []
    for tname in tests:
        body = slice_test_method_body(cs_text, tname) or ""
        if kind == "get_my_clubs_auth":
            route_ids.append(0 if "WhenNoUserClaim" in tname else 1)
        else:
            rid = extract_route_id(body, receiver, csharp_method, kind)
            route_ids.append(rid)

    vals = sorted({v for v in route_ids if v is not None})
    if kind == "get_my_clubs_auth":
        ws.cell(ROW_VAL_LO, 4).value = "No user claim"
        ws.cell(ROW_VAL_HI, 4).value = "Authenticated user"
        lo, hi = 0, 1
    elif kind in ("none", "webhook"):
        ws.cell(ROW_VAL_LO, 4).value = None
        ws.cell(ROW_VAL_HI, 4).value = None
    elif not vals:
        ws.cell(ROW_VAL_LO, 4).value = 1
        ws.cell(ROW_VAL_HI, 4).value = 999
        lo, hi = 1, 999
    elif len(vals) == 1:
        lo = vals[0]
        hi = 999
        ws.cell(ROW_VAL_LO, 4).value = lo
        ws.cell(ROW_VAL_HI, 4).value = hi
    else:
        lo = vals[0]
        hi = vals[-1]
        ws.cell(ROW_VAL_LO, 4).value = lo
        ws.cell(ROW_VAL_HI, 4).value = hi

    n = len(tests)
    for i in range(n):
        c = UTC_START + i
        ws.cell(ROW_PRE, c).value = None
        ws.cell(ROW_DB, c).value = None

        if kind in ("none", "webhook"):
            ws.cell(ROW_VAL_LO, c).value = None
            ws.cell(ROW_VAL_HI, c).value = None
            continue

        rid = route_ids[i]
        if rid is None:
            ws.cell(ROW_VAL_LO, c).value = None
            ws.cell(ROW_VAL_HI, c).value = None
            continue
        if len(vals) <= 1:
            ws.cell(ROW_VAL_LO, c).value = "O" if rid == lo else None
            ws.cell(ROW_VAL_HI, c).value = None
        else:
            if rid == lo:
                ws.cell(ROW_VAL_LO, c).value = "O"
                ws.cell(ROW_VAL_HI, c).value = None
            elif rid == hi:
                ws.cell(ROW_VAL_LO, c).value = None
                ws.cell(ROW_VAL_HI, c).value = "O"
            else:
                ws.cell(ROW_VAL_LO, c).value = None
                ws.cell(ROW_VAL_HI, c).value = "O"

    ws.cell(ROW_RET_OK, 4).value = "T"
    ws.cell(ROW_RET_ERR, 4).value = "F"
    for i in range(n):
        c = UTC_START + i
        tname = tests[i]
        is_n = scenario_type_fn(tname) == "N"
        ws.cell(ROW_RET_OK, c).value = "O" if is_n else None
        ws.cell(ROW_RET_ERR, c).value = None if is_n else "O"
