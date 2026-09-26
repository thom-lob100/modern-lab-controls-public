# ModernFieldList 교체 가이드

- **대체 대상**: 단건 상세 표시용 "캡션/값 표" — `TableLayoutPanel` + 라벨 2N개
  조합(괘선 상세 표), 또는 그 모던판인 `ModernDetailTable`
- **네임스페이스**: `Modern.Lab.WinForms.Controls.Display`

## 언제 무엇을 쓰나

| 상황 | 컨트롤 |
|---|---|
| 단건 정보 카드(Lot 정보, 선택 상세) — 모던 룩 | **ModernFieldList** (캡션 위 + 값 아래 스택형, 괘선/셀 배경 없음) |
| 기존 폼의 괘선 표를 배치 그대로 현대화 | `ModernDetailTable` (TableLayoutPanel 드롭인 — 괘선·캡션 셀 톤 유지) |

## 제공 멤버

| 멤버 | 비고 |
|---|---|
| `Columns` | 열 수 (기본 2). 필드는 왼쪽→오른쪽, 위→아래 |
| `DefineFields(params ModernFieldDefinition[])` | 캡션 + 값 컬럼 이름 쌍으로 배치 정의 (보통 생성자에서 1회). **부르지 않아도 된다** — 아래 |
| `SetRow(DataRow)` | 행에서 값을 채운다. **정의가 없으면 그 행이 속한 표가 필드를 정한다**(2026-08-31) — `DefineFields` 없이 `SetRow`만 불러도 카드가 채워진다. 규칙은 그리드와 같다(`AutoColumns`: 용어사전 캡션 · `_COLOR` 컬럼 제외). 골라 보여 주거나 열 병합·링크가 필요할 때만 `DefineFields`로 선언한다 |
| `ModernFieldDefinition(컬럼[, columnSpan])` | **캡션 생략 — 용어사전에서 읽는다** (2026-08-29 추가). 그리드의 `new ModernDataGridColumn("EQP_ID")`와 같은 규칙(`GridCaptionCatalog` → `CaptionResolver` → 폴백 `"EQP_ID"` → `"Eqp Id"`)이라 그리드 헤더와 상세 카드의 어휘가 한 사전에서 나온다. 화면 문맥상 다른 말이 필요할 때만 캡션을 준다 |
| `ModernFieldDefinition(캡션, 컬럼, columnSpan)` | **열 병합** — 제품명·설명처럼 값이 긴 필드를 한 줄 전체로 넓힌다. 남은 열보다 넓으면 다음 줄로 내려 줄 끝까지 채운다 |
| `ModernFieldDefinition(...) { IsLink = true }` | **링크 필드** (2026-08-29 추가) — 값이 액센트색 + **항상 밑줄**로 그려지고 마우스 오버 시 색이 한 단계 진해지며 손 커서가 된다. 그리드의 `GridColumnKind.Link` 컬럼과 같은 역할(의뢰 번호 → 의뢰서 팝업). 빈 값("-")은 링크가 아니다 |
| `FieldLinkClick` (이벤트, `ModernFieldLinkClickEventArgs.Member/Value`) | 링크 필드의 값을 왼쪽 클릭했을 때. 폼은 `Member`로 어느 필드인지 가르고 `Value`로 팝업을 연다 |
| `ModernFieldDefinition(...) { IsBadge = true }` | **배지 필드** (2026-09-15 추가) — 값 자리에 `ModernStatusBadge`가 놓인다. 배경색은 **값에서 유도**되므로(`ColorValue` → `Palette.FromValue`) 그리드/트리 배지의 `BadgeAutoColor`와 같은 규칙이고, **같은 값이면 표와 카드의 색이 맞는다**. 배지 시각을 카드가 다시 그리지 않고 배지 컨트롤을 그대로 얹는다. 값이 비면 배지 대신 "-"를 그리고, `IsLink`를 함께 켜도 링크로 동작하지 않는다(배지가 값 자리를 가진다) |
| `IsBadge` 의 수명 | `DefineFields` 에 넘기는 **그 시점에 읽힌다** — 카드에 넘긴 뒤에 켜면 반영되지 않는다. 매번 읽는 `IsLink` 와 다르다. 같은 정의를 다시 적용하면(조회마다 `Apply`) 배지를 다시 만들지 않고 쓰던 것을 유지한다 — 회전 위상과 핸들이 보존된다 |
| `BadgeAccentValues` / `BadgeSpinValues` | 배지 필드에서만 쓰인다 — 강조할 값 목록(진한 오류 채움 + 테두리)과 테두리 회전(코멧 빛띠)을 켤 값 목록. 세미콜론/쉼표 구분이고 그리드 배지의 `BadgeAccentValues`·`BadgeSpinValues`와 같은 문법이다. **해시 자동색은 값을 구분해 줄 뿐 의미를 주지 않으므로**(실패가 초록으로 나올 수 있다) 눈에 띄어야 하는 값은 강조로 따로 뺀다 |
| `ModernFieldDefinition(...) { Format = "N0" }` | **값 표시 형식** (2026-09-26 추가) — 그리드 컬럼 `Format`과 같은 문법·규칙이고 같은 서식 함수를 쓴다. 문자열로 온 날짜·숫자도 해석해 적용하며, 해석·적용에 실패하면 원래 값을 보여 준다(예외 없음). 이름이 `_TM`으로 끝나는 필드는 그리드와 같이 `yyyy-MM-dd HH:mm:ss`가 기본값이다. 빈 값을 주면 서식 없이 원래 값을 보여 준다. `SetRow`가 값을 읽을 때마다 적용된다 |
| `SetRow(DataRow)` | 한 건의 행에서 각 필드의 Member 컬럼을 읽어 채움. 없는 컬럼/빈 값 = "-" (예외 없음) |
| `SetValue(member, value)` | 특정 필드 값 직접 지정 (행에 없는 파생 값용) |
| `ClearValues()` | 전부 "-" (선택 해제/초기화) |

## 필드를 안 적는 경우 (2026-08-31)

`DefineFields`를 부르지 않고 `SetRow`만 주면 **조회 결과의 컬럼이 그대로 필드가 된다.**
캡션은 그리드 헤더와 같은 용어사전에서 나온다.

화면 사정(넓게 쓸 칸, 링크로 만들 칸)은 `FieldDefinitions` 빌더로 얹는다.

```csharp
FieldDefinitions.Of(lots)
        .Link("REQ_SERIAL_NO")
        .Span("DESCRIPTION", 5)
        .Apply(this.fieldInfo);
```

| 메서드 | 무엇 |
|---|---|
| `Of(table)` / `Of(row)` | 조회 결과에서 필드 목록을 만든다 |
| `Only(...)` / `Hide(...)` | 남길 필드 / 뺄 필드 |
| `Caption(member, text)` | 용어사전과 다르게 부를 때만 |
| `Span(member, n)` | 여러 칸을 차지하는 넓은 필드(설명 등) |
| `Link(...)` | 클릭 가능한 값 — `FieldLinkClick`으로 받는다 |
| `Badge(...)` | 값을 상태 배지로 — 색은 값에서 유도된다(그리드 배지와 같은 규칙) |
| `BadgeAccent(member, values)` | 그 값일 때만 진한 오류 채움 + 테두리 |
| `BadgeSpin(member, values)` | 그 값일 때만 배지 테두리가 돈다(전송중·처리중) |
| `Format(member, format)` | 값 표시 형식 — 그리드 `GridColumns.Format`과 같은 문법(`"N0"`, `"yyyy-MM-dd"`). `_TM` 필드의 기본 시각 서식을 바꾸거나, 빈 값으로 끌 때도 쓴다 |
| `First(...)` | 맨 앞에 둘 필드 |

필드를 늘리거나 줄이거나 순서를 바꾸는 일은 **쿼리**에서 한다. 없는 이름은 조용히 무시한다.

## 시각 규칙

- **캡션(회색, Regular) 위 + 값(SemiBold) 아래** — 괘선과 셀 배경 없이 굵기·색으로
  위계를 만든다 (DevExpress Detail View / Fluent 설정 화면 관례).
- 행 높이는 컨트롤 Size를 행 수로 등분 — 필드 8개 2열이면 높이 152(행 38) 권장.
- 표시 전용 GDI+ — 라벨 2N개(WPF 섬 2N개)를 쓰던 자리가 컨트롤 1개가 된다.

## 교체 예시 (기존 "괘선 표" 폼)

```csharp
// 변경 전 — TableLayoutPanel + 캡션/값 라벨 16개 + (값 라벨 ↔ 컬럼) 매핑 배열
// 변경 후 — 컨트롤 1개 + 필드 정의
private Modern.Lab.WinForms.Controls.Display.ModernFieldList fieldLotInfo;

this.fieldLotInfo.Columns = 2;
this.fieldLotInfo.DefineFields(
    new ModernFieldDefinition("MODEL_ID", 2),              // 캡션은 사전에서 ("Product"), 한 줄 전체
    new ModernFieldDefinition("STAT_TYP"),                 // "Status"
    new ModernFieldDefinition("Event Tm", "EVENT_TM"));    // 화면 문맥의 말이 따로 있으면 명시

this.fieldLotInfo.SetRow(row);      // 조회 후

// 링크 필드 — 표의 Req Serial No 링크와 같은 팝업을 상세 카드에서도
this.fieldInfo.DefineFields(new ModernFieldDefinition("REQ_SERIAL_NO") { IsLink = true }, ...);
this.fieldInfo.FieldLinkClick += (s, e) => { if (e.Member == "REQ_SERIAL_NO") { this.ShowRequestInfo(e.Value); } };
this.fieldLotInfo.ClearValues();    // 초기화
```

권장 크기: 폭 자유 × (행 수 × 38). 샘플: Manual Receive의 Lot 정보 카드(2열 × 4행),
Lot History의 Selection 상세(`fieldDetail`).
