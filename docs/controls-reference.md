# Modern.Lab 공통 컨트롤 사용법 레퍼런스

컨트롤별 주요 속성·이벤트·메서드의 사용법을 예제와 함께 정리한 문서다.
"기존 컨트롤을 무엇으로 어떻게 교체하는가"는 [migration/](migration/) 폴더의
컨트롤별 교체 가이드를 참고하고, 이 문서는 "교체한 뒤 어떻게 쓰는가"를 다룬다.

- 네임스페이스: 래퍼(WinForms) `Modern.Lab.WinForms.Controls.*`,
  컬럼 정의 등 공용 타입 `Modern.Lab.Controls.Wpf.*`
- 모든 컨트롤 공통: `Enabled` 전파, 디자이너에서 스냅샷 미리보기 표시,
  높이 32(컨트롤), 그리드 행은 28(한 화면에 더 많은 행 — 현장 의견 2026-08-25,
  반응에 따라 32로 원복 가능) / 카드류는 자유. **32는 기본이자 상한이다** — 자동 배치
  (팝업·SizeToContent 창 등 WPF 조합)에서는 32를 요구하고, 호스트가 더 낮은
  높이를 주면(배율 125% 등 고DPI에서 스케일되지 않은 폼 포함) 필드가 그
  높이로 줄어들며, 하단이 잘리지 않는다. 폼 전체가 배율대로 커지길 원하면
  호스트 exe에 DPI 인식 manifest(dpiAware=true)를 선언한다 —
  `Modern.Lab.Samples/app.manifest`가 예시(선언 근거 주석 포함)
- 텍스트 렌더링: WPF 컨트롤은 Display 포매팅 + ClearType (WPF 자체 렌더링이라
  MacType 등 GDI 후킹 도구와 무관하게 동일하게 보임). 경량 GDI+ 컨트롤
  (버튼·라벨·상태 배지·탭·로드 커버)의 글자는 기본 GDI ClearType이며,
  WPF 쪽과 글자 인상을 맞추고 싶으면 `ScaledTextRenderer.TextMode`로
  GDI+ 경로를 선택할 수 있다 (아래 [공통 장평](#공통-장평-fontwidthratio) 표 참고)
- 입력 컨트롤 공통 (`ModernTextBox`/`ModernDatePicker`/`ModernComboBox`/`ModernCheckComboBox`):
  `Required = true`면 **값이 비어 있는 동안만 필드 오른쪽에 작은 빨간 점(●)** 이 표시되고
  입력/선택하면 사라짐 — "아직 안 채운 필수 항목"을 알려주는 상태 기반 표시
  (라벨의 `Required` 별표와 세트)

## 목차

1. [공통 데이터 계약](#공통-데이터-계약)
2. [ModernLabel](#modernlabel) — 라벨
3. [ModernButton](#modernbutton) — 버튼
4. [ModernDropDownButton](#moderndropdownbutton) — 드롭다운 버튼(버튼+메뉴)
5. [ModernCheckBox](#moderncheckbox) — 체크박스
6. [ModernToggleSwitch](#moderntoggleswitch) — 온/오프 토글 스위치
6-1. [ModernToggleButton](#moderntogglebutton) — 버튼 모양 필터 토글
6-2. [ModernToggleGroup](#moderntogglegroup) — 세그먼트 배타 선택
7. [ModernTextBox](#moderntextbox) — 텍스트 입력 + 자동완성
7-1. [ModernPasswordBox](#modernpasswordbox) — 비밀번호 입력
7-2. [ModernFilePathPicker](#modernfilepathpicker) — 파일/폴더 경로 입력
7-3. [ModernRichTextBox](#modernrichtextbox) — 서식 텍스트 편집기(RTF)
8. [ModernNumericTextBox](#modernnumerictextbox) — 숫자/금액 입력
9. [ModernDatePicker](#moderndatepicker) — 날짜 선택
9-1. [ModernDateRangePicker](#moderndaterangepicker) — 기간(From~To) 선택 + 프리셋
9-2. [ModernTimePicker](#moderntimepicker) — 시각(HH:mm) 선택
10. [ModernMonthPicker](#modernmonthpicker) — 년월 선택
11. [ModernComboBox](#moderncombobox) — 콤보(검색형·멀티컬럼)
12. [ModernCheckComboBox](#moderncheckcombobox) — 체크 콤보(다중 선택)
12-1. [ModernCheckedListBox](#moderncheckedlistbox) — 체크 리스트박스(상시 표시 다중 선택)
12-2. [ModernListBox](#modernlistbox) — 리스트박스(상시 표시 단일/다중 선택)
13. [ModernRadioGroup](#modernradiogroup) — 라디오 그룹(배타 선택)
14. [ModernTreeView](#moderntreeview) — 트리(조직도/계층 선택)
14-1. [ModernTreeList](#moderntreelist) — 트리 + 컬럼 하이브리드 (계보 + 값 컬럼)
15. [ModernDataGrid](#moderndatagrid) — 데이터 그리드
16. [ModernPagination](#modernpagination) — 페이지 바
17. [ModernKpiCard / ModernSummaryList](#modernkpicard--modernsummarylist) — 통계 표시
17-1. [ModernStepIndicator](#modernstepindicator) — 진행 단계 표시
17-1. [ModernProgressBar](#modernprogressbar) — 확정형 진행 바
17-2. [ModernFieldList](#modernfieldlist) — 단건 상세 필드 목록
18. [ModernStatusBadge](#modernstatusbadge) — 상태 배지
18-1. [ModernSlotMap](#modernslotmap) — 캐리어 슬롯 맵 (수납 구조 단면)
18-2. [ModernHandlerLayout](#modernhandlerlayout) — 핸들러 구조도
18-3. [ModernSlotMapMini](#modernslotmapmini) — 미니 수납 현황 맵 (FOUP 슬롯/STUB/LCC)
18-4. [ModernRequestInfo](#modernrequestinfo--제거됨-2026-08-12) — **제거됨** — 샘플 의뢰서 다이얼로그(`RequestInfoDialogForm`)로 이전
18-5. [ModernBarcode](#modernbarcode) — Code 128 바코드 표시
19. [ModernBusyOverlay](#modernbusyoverlay) — 로딩 오버레이
20. [ModernToast](#moderntoast) — 자동 소멸 알림
20-1. [ModernMessageDialog](#modernmessagedialog) — 모던 메시지/확인 다이얼로그
20-1. [ModernPopover](#modernpopover) — 필요할 때만 띄우는 팝오버(레이아웃 비점유)
20-2. [ModernToolTip](#moderntooltip) — 테마 일치 툴팁
20-3. [ModernMenuRenderer](#modernmenurenderer) — 컨텍스트 메뉴 모던 렌더러(상자 없는 체크)
21. [ModernCardPanel](#moderncardpanel) — 카드 판넬
22. [ModernGroupBox](#moderngroupbox) — 타이틀 있는 카드(그룹박스)
22-1. [ModernSplitContainer](#modernsplitcontainer) — 크기 조절 스플리터
22-2. [ModernTabControl](#moderntabcontrol) — 언더라인 탭
22-3. [ModernExpander](#modernexpander) — 접이식 그룹박스
23. [ModernDataGridColumn](#moderndatagridcolumn) — 컬럼 정의 (그리드/콤보 공용)
24. [테마 (ModernTheme)](#테마-moderntheme--라이트다크--틴트-5종) — 라이트/다크/오렌지블루/그린토마토/크림슨그레이/블루/라이트퍼플
25. [문구 작성 기준](#문구-작성-기준) — 라벨·버튼·토스트·빈 상태·로딩 문구의 표기 규칙

---

## 공통 데이터 계약

데이터 바인딩 컨트롤(콤보·체크콤보·그리드·요약 목록)은 전부 같은 규칙을 따른다.

| 규칙 | 의미 |
|---|---|
| `DataSource` 수용 형식 | `DataTable` / `DataView` / `IList` / `IEnumerable` — 서버 응답을 그대로 할당 |
| 순서 내성 | `SelectedValue`(또는 `CheckedValues`)를 `DataSource`보다 **먼저** 설정해도 됨 — 보류했다가 데이터 도착 시 적용 |
| 재할당 리셋 | `DataSource`를 다시 할당하면 선택/체크가 깨끗하게 초기화되고 변경 이벤트는 정확히 1회 발생 |
| null 안전 | null/빈 데이터는 빈 목록으로 표시, 예외 없음 |
| 누락 컬럼 자동 보장 | `DataTable`/`DataView` 소스에서 컨트롤이 참조하는 컬럼(그리드의 `ConfigureColumns` 정의·`RowColorMember`, 트리의 `IdMember` 등)이 없으면 빈 문자열 컬럼으로 자동 추가 — JSON→DataTable 변환이 전부-null 컬럼을 생략해도 폼에서 컬럼 목록을 다시 나열할 필요 없음 |
| 스레드 | 백그라운드 조회 후 `this.Invoke(...)`로 UI 스레드에서 할당하는 기존 패턴 그대로 동작 |
| 수동 데이터 | 폼이 데이터를 조회해서 할당한다 — 컨트롤은 서버를 모른다 |

```csharp
// 전형적인 서버 조회 패턴 — 기존 폼 코드와 동일
private void OnSearchClick(object sender, EventArgs e)
{
    DataTable reply = this.CallServer("EMP_LIST", this.BuildRequest());  // 폼의 기존 통신 코드
    this.gridEmployee.DataSource = reply;                                // 할당만 하면 끝
}
```

---

## 공통 장평 (FontWidthRatio)

글자 가로 비율(장평)을 전역 토큰 + 컨트롤별 재정의로 조절한다.
허용 범위는 **0.8~1.2**이며 범위 밖 값은 경계로 잘린다.

```csharp
// 전역: Program.cs에서 첫 컨트롤 생성 전에 한 번 (테마 Mode와 같은 규칙)
Modern.Lab.Theming.ModernTheme.FontWidthRatio = 0.9;   // 90% 축소 장평

// 컨트롤별 재정의: 0(기본) = 전역 사용, 양수 = 이 컨트롤만 해당 비율
this.gridHistory.FontWidthRatio = 1.1;
```

| 항목 | 내용 |
|---|---|
| 전역 토큰 | `ModernTheme.FontWidthRatio` — 앱 시작 시 한 번 설정 (WPF 쪽은 컬럼/템플릿 구성 시점에 확정) |
| 재정의 지원 | **모든 모던 컨트롤** — ElementHost 래퍼는 `WpfElementHostBase.FontWidthRatio` 공통 속성, GDI+ 컨트롤(`ModernLabel`/`ModernStatusBadge`/`ModernGroupBox`/`ModernTabControl`)은 개별 속성. 이름은 모두 `FontWidthRatio`, 0 = 전역 |
| 적용 범위 | 입력류 내부 텍스트·플레이스홀더(TextBox/Numeric/DatePicker/MonthPicker/ComboBox/CheckCombo), 콤보·라디오·체크·토글 항목 라벨, 트리 노드, 그리드 셀/헤더/배지/버튼 캡션/상태바(+AutoFit 측정), 페이지네이션, 스텝 라벨, 버튼/드롭다운 캡션, 토스트/로딩 메시지, KPI/요약 텍스트, GDI 라벨/배지/그룹 타이틀/탭 헤더 |
| 제외 | 아이콘 글리프(Segoe MDL2), 달력 팝업 내부(표준 Calendar) — 글자가 아니라 스케일하지 않는다 |
| 구현 방식 | WPF = `FontWidthScaling` 첨부 속성(상속되는 가로 ScaleTransform을 텍스트 요소가 `LayoutTransform`으로 바인딩), GDI+ = `ScaledTextRenderer`(DrawString + 가로 스케일, ClearTypeGridFit). GDI `lfWidth` 방식은 한글 폰트 링크가 깨져 쓰지 않는다 |
| 샘플 확인 | `Modern.Lab.Samples.exe --fontwidth=0.9` (또는 1.1, 1.2) |

### GDI 텍스트 렌더링 모드 (ScaledTextRenderer.TextMode)

경량 GDI+ 컨트롤(버튼·라벨·상태 배지·탭·로드 커버)의 **글자 래스터라이저**를 고른다.
기본(GDI)은 윈도우 순정 컨트롤과 같은 강한 힌팅의 ClearType이고, 같은 화면의 WPF
컨트롤(그리드·콤보)과 글자 인상이 미세하게 달라 거슬리면 GDI+ 경로로 바꿔 맞출 수 있다.

```csharp
// Program.cs에서 첫 컨트롤 생성 전에 한 번 (테마 Mode와 같은 규칙)
Modern.Lab.WinForms.Rendering.ScaledTextRenderer.TextMode =
        Modern.Lab.WinForms.Rendering.GdiTextRenderingMode.GdiPlusClearType;
```

| 값 | 렌더링 | 인상 |
|---|---|---|
| `Gdi` (기본) | GDI TextRenderer ClearType | 탐색기·메모장과 같은 윈도우 순정 글자. 힌팅이 강해 또렷 |
| `GdiPlusClearType` | GDI+ DrawString + ClearTypeGridFit | 힌팅이 약해 WPF(Display) 글자 인상에 가깝다 |
| `GdiPlusGrayscale` | GDI+ DrawString + AntiAliasGridFit | 그레이스케일 AA — 서브픽셀 색 프린지 없음, 가장 부드럽다 |

샘플 확인: `Modern.Lab.Samples.exe --gditext=cleartype` / `--gditext=grayscale`
(창 제목에 모드가 표기되므로 기본 실행과 나란히 놓고 비교한다).

---

## ModernLabel

텍스트 라벨. `Font`/`ForeColor`를 직접 지정하는 대신 **역할(Kind)** 을 고른다.

글자는 남는 세로 여백이 홀수일 때 **1px 아래**로 그려진다 — 같은 줄의 `ModernTextBox`·콤보·날짜 칸과
글자 높이를 맞추기 위한 값이다(2026-09-22, 근거는 `docs/migration/ModernLabel.md` 「세로 위치」). 라벨과
입력 칸을 같은 `Location.Y` · 같은 `Height`(32)로 두면 그대로 맞는다. 보정이 `ModernLabel` 에만 있어
`ModernStatusBadge` 등 GDI+ 이웃과는 1px 차이가 남는다.

### 속성

| 속성 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `Text` | string | "레이블" | 표시 텍스트. `Control.Text` override라 기존 코드/리소스 그대로 동작 |
| `Kind` | `LabelKind` | `Body` | 타이포그래피 역할 — 크기·굵기·색이 자동 결정 |
| `Required` | bool | false | true면 텍스트 뒤에 빨간 별표(\*) 표시 (필수 입력 표시) |
| `TitleBar` | bool | false | `Kind=Title`일 때 텍스트 왼쪽에 액센트색 세로 타이틀 바 표시. Title이 아니면 무시됨 |
| `CaptionMember` | string | "" | 지정하면 **용어사전 캡션을 그린다**(그리드 컬럼과 같은 규칙 — `GridCaptionCatalog` → 폴백 `"EQP_ID"` → `"Eqp Id"`). `Text`는 건드리지 않아 디자이너 직렬화 순서와 무관하고 앱 시작 후 등록된 사전을 따른다. `ModernDetailTable`의 캡션 셀처럼 라벨이 곧 캡션인 자리용 (2026-08-29 추가) |
| `Hyperlink` | bool | false | 하이퍼링크 모드 — 액센트색 + 밑줄 + 손 커서, 호버 시 한 단계 진한 액센트. 클릭은 표준 `Click` 이벤트로 받는다 (`LinkLabel` 대체) |

`Kind` 값:

| 값 | 모양 | 용도 |
|---|---|---|
| `Body` | 12px Regular, 진한 색 | 일반 텍스트 |
| `Title` | 16px SemiBold | 섹션 제목 |
| `Label` | 12px SemiBold, 회색 | 입력 필드 앞 라벨 |
| `Helper` | 12px Regular, 회색 | 보조 설명 |

### 예제

```csharp
this.lblName.Kind = Modern.Lab.Controls.Wpf.Display.LabelKind.Label;
this.lblName.Text = "이름";
this.lblName.Required = true;   // "이름 *" 로 표시

this.lblPageTitle.Kind = Modern.Lab.Controls.Wpf.Display.LabelKind.Title;
this.lblPageTitle.TitleBar = true;   // "▎Lot Management" 처럼 왼쪽에 파란 세로 바
this.lblPageTitle.Text = "Lot Management";

// 하이퍼링크 라벨 — 클릭하면 상세/이력 화면을 여는 관례 (LinkLabel 대체)
this.valLotId.Hyperlink = true;
this.valLotId.Click += this.OnLotIdClick;
```

---

## ModernButton

버튼. 색을 직접 칠하는 대신 **위계(Kind)** 를 고른다.
GDI+로 직접 그리는 순수 WinForms 컨트롤이며(ElementHost 아님), 색은 WPF
컨트롤과 같은 토큰 사전에서 읽는다 — 테마 전환과 시각 결과는 동일하다.

### 속성·이벤트

| 멤버 | 타입 | 설명 |
|---|---|---|
| `Text` | string | 캡션. `Control.Text` override |
| `Kind` | `ButtonKind` | 버튼 위계 (아래 표) |
| `IconGlyph` | string | Segoe MDL2 Assets 글리프 (예: `""` 저장 아이콘). 비우면 아이콘 없음 |
| `GlyphSize` | double | 캡션/아이콘 글자 크기 재정의(px, 0 = 토큰 기본). 값이 있으면 좁은 아이콘 버튼에서 글리프가 잘리지 않게 좌우 패딩이 줄어든다 |
| `TopLabel` | string | 글리프/캡션 **위**의 아주 작은 상단 라벨(비우면 숨김) — "All"/"Selected" 같은 부가 설명. 값이 있으면 두 줄에 맞춰 버튼 높이가 늘어난다 |
| `BadgeCount` | int | 우상단 카운트 배지 (기본 0 = 없음) — 미처리 건수 알림용 빨간 알약. 99 초과는 "99+" |
| `Click` | 이벤트 | 표준 WinForms 이벤트 — 기존 핸들러 그대로 |
| `DialogResult` | `DialogResult` | 누르면 폼의 `DialogResult`에 설정 (기본 `None` = 폼 결과를 건드리지 않음) |
| `PerformClick()` | 메서드 | 클릭을 프로그램적으로 발생. 비활성이거나 **숨겨진** 버튼(그런 부모 아래 포함)은 무동작 — 순정 버튼과 같은 `CanSelect` 판단 |
| `NotifyDefault(bool)` | 메서드 | 폼이 기본 버튼으로 지정하면 호출 — 테두리 안쪽 링으로 표시 |

`IButtonControl`을 구현하므로 **폼의 `AcceptButton`/`CancelButton`에 그대로 지정된다**
(2026-08-04). 다이얼로그의 Enter/Esc를 위해 `ProcessDialogKey`를 재정의하지 않는다 —
`.Designer.cs`에 두 줄이면 된다:

```csharp
this.AcceptButton = this.btnOk;
this.CancelButton = this.btnCancel;
```

Enter 대상이 화면 상태에 따라 갈리는 경우(확인 전/후로 다른 버튼)만 `ProcessDialogKey`를
남기고, 그때도 Esc는 `CancelButton`이 맡는다.

`Kind` 위계 (화면당 `Primary`와 `Execute`는 각각 1개 권장):

| 값 | 모양 | 용도 |
|---|---|---|
| `Primary` | 파랑 채움 | 화면의 대표 동작 (조회) |
| `Execute` | 초록 채움 | 중요 실행 동작 (실행/승인) |
| `Secondary` | 흰 배경 + 회색 외곽선 | 일반 동작 (신규/저장/수정) |
| `Danger` | 빨강 글자·외곽선 | 파괴적 동작 (삭제) |
| `Subtle` | 테두리 없는 텍스트 | 저강조 동작 (초기화) |
| `Excel` | 초록 글자·외곽선 (hover 시 옅은 초록 채움; 테마별 `Brush.Excel*` 토큰) | 엑셀 내보내기 전용 포인트 |

### 예제

```csharp
this.btnSearch.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Primary;
this.btnSearch.Text = "조회";
this.btnSearch.Click += this.OnSearchClick;

this.btnDelete.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Danger;
this.btnDelete.Text = "삭제";
```

---

## ModernDropDownButton

버튼 + 메뉴 (`Button`+`ContextMenuStrip` 조합 대체). 클릭하면 항목 메뉴가 열리고,
항목 클릭 시 `ItemClicked`가 발생한다 — 엑셀 내보내기 옵션처럼 한 버튼에 여러 동작을
묶을 때 사용. 캡션 옆 셰브런(▼)은 자동으로 붙는다.

### 속성 / 이벤트

| 멤버 | 설명 |
|---|---|
| `Text` | 버튼 캡션 (`Control.Text` override, localizable) |
| `DataSource` / `DisplayMember` / `ValueMember` | 메뉴 항목 — 공통 데이터 계약과 동일. 표시 텍스트가 **`"-"`인 행은 구분선**으로 그려진다(클릭 불가) — `ToolStripSeparator` 관례로, 컨텍스트 메뉴와 같은 자리에 분리선을 둘 때 쓴다 |
| `EnabledMember` | 항목 실행 가능 여부 컬럼/속성 이름 (bool 또는 `"Y"`/`"true"`/`"1"`). 비우면 전부 활성. 비활성 항목은 회색으로 표시되고 클릭되지 않는다 — 컨텍스트 메뉴의 비활성 표시와 같은 의미. 활성 판정이 상태에 따라 변하면 값 갱신 후 `DataSource`를 다시 할당한다 |
| `Kind` | 버튼 시각 종류 — `ButtonKind.Secondary`(기본, 흰 배경) / `Execute`(Success 초록 채움; 실행 버튼 강조) / `Excel`(초록 아웃라인; 엑셀 내보내기 포인트). 캡션은 `ModernButton`과 같은 일반(Normal) 굵기 |
| `IsDropDownOpen` | 읽기 전용 bool. 메뉴 팝업이 열려 있으면 true — 자동 갱신 화면은 열려 있는 동안 재바인딩을 보류하는 데 사용 |
| `ItemClicked` | 항목 클릭 시 발생. `e.Value`(코드) / `e.DisplayText`(명칭) 제공 |

```csharp
this.ddExcel.Text = "엑셀";
this.ddExcel.DisplayMember = "EXPORT_NAME";
this.ddExcel.ValueMember = "EXPORT_CODE";
this.ddExcel.DataSource = exportTable;   // PAGE 현재 페이지 / ALL 전체 / CSV ...
this.ddExcel.ItemClicked += this.OnExcelItemClicked;

private void OnExcelItemClicked(object sender, DropDownItemClickedEventArgs e)
{
    string code = e.Value as string;   // 분기 처리
}
```

버튼 모양은 보조(Secondary) 스타일 고정. 권장 크기: 100~130×32.

---

## ModernCheckBox

단독 체크박스 (`CheckBox` 대체). 둥근 사각 박스 + 액센트 채움 + 흰 체크 글리프 —
체크콤보 드롭다운 항목과 동일한 비주얼.

### 속성 / 이벤트

| 멤버 | 타입 | 설명 |
|---|---|---|
| `Text` | string | 박스 옆 레이블 (`Control.Text` override, localizable) |
| `Checked` | bool | 체크 상태 |
| `CheckedChanged` | event | 상태가 바뀔 때 1회 발생 (같은 값 재할당은 미발생) |

```csharp
this.chkRecentOnly.Text = "2020년 이후 입사";
this.chkRecentOnly.CheckedChanged += this.OnRecentOnlyCheckedChanged;

if (this.chkRecentOnly.Checked)
{
    conditions.Add("HIRE_DATE >= '2020-01-01'");
}
```

3상태(Indeterminate)는 미지원. 권장 크기: 폭은 텍스트에 맞게, 높이 24.

---

## ModernToggleSwitch

온/오프 토글 스위치 — `ModernCheckBox`와 **동일한 API**(`Text`/`Checked`/`CheckedChanged`)에
비주얼만 알약 트랙 + 원형 썸. **설정성 "켬/끔"** 용도 (다중 선택/포함에는 체크박스).

```csharp
this.tglShowEmail.Text = "이메일 표시";
this.tglShowEmail.Checked = true;
this.tglShowEmail.CheckedChanged += this.OnShowEmailToggled;   // 켬/끔에 따라 화면 구성 변경
```

권장 크기: 폭은 텍스트에 맞게, 높이 24.

---

## ModernToggleButton

버튼 모양 토글 — `CheckBox.Appearance = Button`의 드롭인 대체. 조회 조건 스트립의
필터 토글("내 작업만")·뷰 전환용이다. 꺼짐 = Secondary 버튼, 켜짐 = 액센트 채움.
ModernButton과 같은 GDI+ 직접 그리기라 여러 개 놓아도 섬 비용이 없다.

### 속성 / 이벤트

| 멤버 | 타입 | 설명 |
|---|---|---|
| `Text` | string | 캡션 (override, localizable) |
| `Checked` | bool | 켬(눌림) 상태 — 클릭/Space/Enter로 반전 |
| `CheckedChanged` | 이벤트 | `Checked` 변경 시 1회 발생 |

역할 구분: 설정성 켬/끔은 `ModernToggleSwitch`, 라디오 배타 선택은 `ModernRadioGroup`.

```csharp
this.chkMyTasksOnly.CheckedChanged += this.OnFilterChanged;
bool onlyMine = this.chkMyTasksOnly.Checked;
```

권장 크기: 캡션 폭 + 32px 여유 × 32. 샘플: Control Gallery — Input 탭의 ToggleButton.

---

## ModernToggleGroup

세그먼트형 배타 토글 — 버튼들이 한 덩어리로 붙은 단일 선택.
`RadioButton.Appearance = Button` 묶음의 드롭인 대체이며 조회 조건의 소수 항목
배타 필터(All/Y/N, 그룹 전환)에 쓴다. GDI+ 직접 그리기라 섬 비용이 없다.

### 속성 / 이벤트 (ModernRadioGroup과 같은 데이터 계약)

| 멤버 | 타입 | 설명 |
|---|---|---|
| `DataSource` | object | DataTable/IList/IEnumerable (공통 데이터 계약) |
| `DisplayMember` / `ValueMember` | string | 표시/값 컬럼 (빈 값 = ToString()/항목 자체) |
| `SelectedValue` | object | 선택 값 — DataSource보다 먼저 설정해도 적용(룰 3) |
| `SelectedValueChanged` | 이벤트 | 선택이 실제로 바뀔 때 1회 |

키보드 ←/→로 선택 이동. 다중 선택이 필요하면 `ModernCheckComboBox`,
항목이 많으면 `ModernRadioGroup`.

권장 크기: 항목 수 × 45~65px × 32. 샘플: Logistics & Request의 Sent 필터.

---

## ModernTextBox

텍스트 입력. 플레이스홀더·Enter 조회·자동완성(초성 검색 포함) 내장.
`Multiline = true`면 비고/특이사항용 여러 줄 입력이 되고,
`Mask`를 지정하면 `MaskedTextBox`식 마스크 입력이 된다.

### 속성·이벤트

| 멤버 | 타입 | 설명 |
|---|---|---|
| `Text` | string | 입력 텍스트. 한글 조합 중에는 음절 확정 시점에 반영 |
| `PlaceholderText` | string | 비어 있을 때 회색 힌트. 자음 하나만 쳐도 즉시 사라짐 |
| `ReadOnly` | bool | 읽기 전용. 배경·테두리·글자 세 곳이 함께 바뀐다 — 옅은 배경 + 흐린 테두리 + 보조 텍스트 색(옅은 배경 대비 4.6:1로 WCAG AA를 넘는다). 키보드 포커스가 오면 테두리가 보이되 액센트가 아니라 한 단계 약한 색이라, 탭으로 훑어도 위치를 잃지 않으면서 "고칠 수 있는 칸"과는 구분된다. 비활성(`Enabled = false`)과는 배경이 다르다 |
| `Multiline` | bool | 여러 줄 입력 모드. Enter = 줄바꿈(`EnterPressed` 미발생), 자동 줄바꿈 + 세로 스크롤, 높이는 Size 그대로, 자동완성 비활성. 다이얼로그의 Enter 확인 키와 겹치면 `ContainsFocus`로 가로채기를 피할 것 |
| `MaxLength` | int | 최대 입력 글자 수 (0 = 제한 없음) |
| `Mask` | string | 입력 마스크 — `MaskedTextBox.Mask`와 같은 마스크 언어 (`0` 필수 숫자, `9` 선택 숫자, `L` 필수 영문, `A` 필수 영숫자, `&` 임의 문자, `>`/`<` 대/소문자 강제, 그 외는 리터럴; 예: `">LLL-0000"` = 대문자 영문 3 + `-` + 숫자 4). **자릿수 고정 코드 입력용** — 모양만 강제하고 값 유효성은 검증하지 않으므로 날짜 입력은 `ModernDatePicker`를 쓴다. 지정하면 미입력 자리가 `_`로 표시되고 타이핑·붙여넣기·삭제 전부 마스크를 통과. IME·자동완성은 마스크 동안 비활성. `Text`는 화면 표시 문자열 그대로(리터럴+`_` 포함). 빈 값 = 마스크 없음 |
| `MaskCompleted` | bool (읽기) | 마스크의 필수 자리가 전부 채워졌는지 (마스크 없으면 항상 true) — 저장 전 검증용 |
| `ButtonGlyph` | string | 필드 안 트레일링 버튼 글리프(Segoe MDL2, 예: 돋보기 `"\uE721"`). 비어 있으면 버튼 없음 |
| `ButtonClick` | 이벤트 | 트레일링 버튼 클릭 시 발생 — 기존 별도 조회 버튼의 `Click` 핸들러를 그대로 연결 |
| `ShowClearButton` | bool | **값이 있을 때 지우기(✕) 버튼** 표시 (기본 false) — 검색어/경로처럼 한 번에 비우는 동선용. 값이 비면 사라지고, 트레일링 버튼과 함께면 그 왼쪽에 선다. 지우면 `TextChanged` 발생 + 포커스는 에디터로 |
| (잘림 툴팁) | 자동 | 내용이 필드 폭보다 길면 호버 시 전체 값 툴팁 — 그리드 셀의 잘림 툴팁과 같은 문법 (안 잘렸으면 툴팁 없음). 긴 경로를 담는 `ModernFilePathPicker`에도 그대로 적용 |
| `CharacterCasing` | `CharacterCasing` | 입력 즉시 대문자/소문자 강제 (WinForms 호환 이름, 기본 Normal). 코드/ID 입력용 |
| `AllowedCharacters` | string | 허용 문자 집합 (빈 값 = 제한 없음). 지정하면 타이핑·붙여넣기·IME 무관하게 그 외 문자가 제거됨. 예: `"ABC…XYZ0123456789.-"` |
| `TextChanged` | 이벤트 | 표준 WinForms 이벤트 |
| `EnterPressed` | 이벤트 | Enter 키 입력 시 발생 — **기존 `KeyDown`으로 Enter를 잡던 코드는 이 이벤트로 교체** (WPF 에디터가 처리한 키는 WinForms `KeyDown`으로 오지 않음) |
| `SuggestionClicked` | 이벤트 | 자동완성 후보를 마우스로 클릭해 확정했을 때 발생 — `Text`는 이미 후보. 키보드 확정(↓/↑ + Enter)은 `EnterPressed`로 오므로 "고르면 바로 조회"는 두 이벤트를 같은 핸들러에 건다 (샘플: LotHistoryForm) |
| `AutoCompleteMode` | `AutoCompleteMode` | `None` 외 값은 모두 제안 드롭다운(Suggest)으로 동작 |
| `AutoCompleteSource` | `AutoCompleteSource` | **`CustomSource`만 지원** |
| `AutoCompleteCustomSource` | `AutoCompleteStringCollection` | 후보 목록. 재할당으로 갱신 — 입력 중(포커스 상태) 재할당하면 드롭다운이 즉시 다시 필터링되므로, `TextChanged` + 디바운스 + 서버 조회로 **typeahead** 구현 가능. 단 **추천을 확정(Enter/클릭)했거나 Esc·`CloseSuggestions()`로 닫은 뒤에는 다시 타이핑하기 전까지 재할당이 목록을 다시 열지 않는다** — 확정 값이 다른 후보의 접두사일 때(자식 랏 등) 확정이 유발한 재조회 응답이 드롭다운을 도로 여는 것을 막는다 |
| `CloseSuggestions()` | 메서드 | 열린 추천 드롭다운을 닫고, 다시 타이핑하기 전까지 늦게 도착한 typeahead 응답이 목록을 다시 열지 않게 한다 — 검색 실행 시점에 호출 |
| `SuggestionFilterMode` | `SuggestionFilterMode` | 추천 후보의 클라이언트 측 재대조 방식 (기본 `Contains` = 입력 문자열 포함 매칭, 정적 소스 패턴용). **디바운스 서버 typeahead에는 `None`** — 서버가 이미 거른 목록을 재대조 없이 그대로 띄운다. 서버가 후보 텍스트에 보이지 않는 컬럼(의뢰번호 등)으로 매칭해 돌려주면 `Contains`가 전부 걸러내 드롭다운이 열리지 않으므로 반드시 `None` (샘플: LotHistoryForm). 음소거 래치는 모드와 무관 |

### 예제 — 검색 입력 + 자동완성

```csharp
this.txtName.PlaceholderText = "이름 검색";
this.txtName.EnterPressed += this.OnSearchClick;   // Enter로 바로 조회

// 자동완성: 기존 WinForms TextBox와 동일한 3종 세트
AutoCompleteStringCollection names = new AutoCompleteStringCollection();
foreach (DataRow row in employeeTable.Rows)
{
    names.Add(row["EMP_NAME"].ToString());
}
this.txtName.AutoCompleteMode = AutoCompleteMode.Suggest;
this.txtName.AutoCompleteSource = AutoCompleteSource.CustomSource;
this.txtName.AutoCompleteCustomSource = names;
// → "ㄱ"만 쳐도 김민수/김하늘/강태민 제안 (초성 검색), ↓/↑ 탐색, Enter 선택
```

### 예제 — 입력 마스크 (MaskedTextBox 대체)

```csharp
this.txtRecipe.Mask = ">LLL-0000";       // 대문자 영문 3 + 숫자 4 (예: ABC-1234)
this.txtBizNo.Mask = "000-00-00000";     // 사업자번호 — 숫자 10자리 + 리터럴 '-'

// 리터럴 없는 소문자 원본을 넣어도 마스크에 안착한다 (> = 대문자 강제)
this.txtRecipe.Text = "abc1234";         // → "ABC-1234"

if (!this.txtRecipe.MaskCompleted)       // 저장 전 검증 (필수 자리 모두 채움?)
{
    this.ShowToast("Enter the full recipe code.", ToastKind.Warning);
    return;
}
```

날짜 입력에는 마스크를 쓰지 않는다 — 마스크는 "숫자 8자리 + 구분자" 같은
**모양**만 강제할 뿐 `9999-99-99`도 통과한다. 날짜는 `ModernDatePicker`
(자동 형식화 + 유효성 처리)가 담당한다.

---

## ModernPasswordBox

비밀번호 입력 필드 (`TextBox.UseSystemPasswordChar`/`PasswordChar` 대체).
입력 문자는 ●로 가려지고, 값은 보안 규칙에 따라 CLR 속성으로만 노출된다
(디자이너 직렬화 없음).

### 속성 / 이벤트

| 멤버 | 설명 |
|---|---|
| `Text` / `Password` | 비밀번호 값 (동일 값의 별칭) — 기존 `txtPwd.Text` 코드 그대로. 디자이너에 표시/저장되지 않음 |
| `MaxLength` | 최대 입력 길이 (0 = 제한 없음) |
| `Placeholder` | 입력 전 안내 문구 |
| `Required` | 필수 입력 — 값이 비어 있는 동안 오른쪽 빨간 점 (입력 컨트롤 공통) |
| `TextChanged` | 값 변경 시 — 로그인 버튼 활성화 연동 |
| `Clear()` / `FocusEditor()` | 값 비우기 / 에디터 포커스 |

```csharp
// 로그인 폼 전형
this.txtPassword.Required = true;
this.txtPassword.TextChanged += (s, e) =>
        this.btnLogin.Enabled = this.txtPassword.Text.Length > 0;

string password = this.txtPassword.Text;   // 인증 요청에 사용
this.txtPassword.Clear();                  // 사용 후 비우기
```

---

## ModernFilePathPicker

파일/폴더 경로 입력 필드 — "TextBox + 찾아보기 Button" 조합 대체 (컨트롤 1개).
`ModernTextBox` 상속이라 그 멤버 전부(`Placeholder`/`Required` 등)를 그대로 쓰고,
트레일링 폴더 버튼이 표준 대화상자를 띄운다.

### 속성

| 멤버 | 설명 |
|---|---|
| `Text` / `Path` | 입력/선택된 경로 (동일 값의 별칭). 직접 타이핑도 가능 |
| `PickMode` | `OpenFile`(기본) / `SaveFile` / `Folder` |
| `Filter` | 파일 필터 — `"Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"` |
| `DialogTitle` | 대화상자 제목 (비우면 시스템 기본) |
| `ShowClearButton` | 값이 있으면 지우기(✕) 버튼 표시 — **기본 켜짐** (ModernTextBox 공통 기능; 일반 텍스트박스는 기본 꺼짐) |

```csharp
this.pickImport.PickMode = FilePathPickMode.OpenFile;
this.pickImport.Filter = "CSV files (*.csv)|*.csv";
string path = this.pickImport.Text;   // TextChanged로 변경 감지
```

---

## ModernRichTextBox

서식 텍스트 편집기 — `RichTextBox`의 대체 (WPF RichTextBox 토큰 래핑,
경량안). 굵게/기울임/밑줄은 **상단 서식 툴바(B/I/U 토글 — 선택 위치 상태를
되비춤)** 또는 표준 단축키(Ctrl+B/I/U). 검사 소견처럼 "서식 있는 메모"를
저장/표시하는 화면용.

| 멤버 | 설명 |
|---|---|
| `Rtf` | RTF 문자열 콘텐츠 — DB 저장/조회 왕복용. 비RTF 값은 텍스트로 수용, 손상 RTF는 원문 표시(예외 없음) |
| `Text` | 일반 텍스트 콘텐츠 (서식 없이). 끝쪽 빈 줄 포함 개행 수가 왕복 보존되며 `\r\n`으로 정규화 |
| `ReadOnly` | 편집만 잠금 (선택/복사 가능, 툴바도 잠김) |
| `ShowToolbar` | 서식 툴바 표시 여부 (기본 true) — 표시 전용 뷰어는 끔 |
| `TextChanged` | 표준 이벤트 |
| `LoadFile(path)` / `SaveFile(path)` / `Clear()` | RTF 파일 로드/저장, 비우기 |

```csharp
this.txtRemark.Rtf = row["REMARK_RTF"].ToString();   // 조회
parameters["REMARK_RTF"] = this.txtRemark.Rtf;       // 저장
```

`SelectionFont` 같은 선택 영역 서식 API는 없다 — 자세한 대체는
`migration/ModernRichTextBox.md`. 샘플: **Rich Text** 데모.

---

## ModernNumericTextBox

금액/수량 입력 필드 (`NumericUpDown`·숫자용 TextBox 대체). 숫자만 치면 **천단위 콤마가
자동 삽입**되고(`1234567` → `1,234,567`) 우측 정렬로 표시된다. 중간 수정·붙여넣기에도
즉시 재형식화되며 숫자 외 문자는 무시된다.

### 속성 / 이벤트

| 멤버 | 타입 | 설명 |
|---|---|---|
| `Value` | `decimal?` | 입력 값. **`null` = 미입력(전체 조회)** — 초기화 시 `null` 할당 |
| `DecimalPlaces` | int | 허용 소수 자릿수 (기본 0 = 정수만). blur 시 자릿수 패딩(`1,234.50`) |
| `AllowNegative` | bool | 음수 허용 (기본 true) |
| `PlaceholderText` | string | 입력 전 회색 안내 (예: "만원") |
| `Required` | bool | 필수 입력 표시 (값이 비어 있는 동안 빨간 점) |
| `ValueChanged` | event | 값이 바뀔 때 1회 발생 |

```csharp
this.numSalaryFrom.PlaceholderText = "만원";
decimal? from = this.numSalaryFrom.Value;   // 조회 시
this.numSalaryFrom.Value = null;            // 초기화
```

권장 크기: 100~140×32.

---

## ModernDatePicker

날짜 선택 필드 (`DateTimePicker` 대체, 날짜 전용). 달력 팝업 또는 직접 타이핑으로 입력한다.
직접 입력은 **마스크 방식**: 숫자만 치면 `yyyy-MM-dd`로 자동 형식화되고(구분자 입력 불필요),
중간 위치를 수정해도 즉시 재형식화되며 무효 입력은 오류 없이 값 미반영으로 처리된다.

### 속성 / 이벤트

| 멤버 | 타입 | 설명 |
|---|---|---|
| `Value` | `DateTime?` | 선택 날짜. **`null` = 미선택(전체 조회)** — 초기화 시 `null` 할당 |
| `MinDate` / `MaxDate` | `DateTime?` | 달력에서 선택 가능한 범위 (`null` = 제한 없음) |
| `PlaceholderText` | string | 입력 전 회색으로 표시되는 형식 안내 (기본 `"yyyy-MM-dd"`) |
| `Required` | bool | 필수 입력 표시 (값이 비어 있는 동안 필드 오른쪽 빨간 점) |
| `ValueChanged` | event | 날짜가 바뀔 때 1회 발생 (달력·타이핑·코드 할당 공통) |

### 예제 — 구간(범위) 조회 조건

```csharp
DateTime? from = this.dtHireFrom.Value;
DateTime? to = this.dtHireTo.Value;

if (from.HasValue) { conditions.Add("HIRE_DATE >= '" + from.Value.ToString("yyyy-MM-dd") + "'"); }
if (to.HasValue) { conditions.Add("HIRE_DATE <= '" + to.Value.ToString("yyyy-MM-dd") + "'"); }

// 초기화
this.dtHireFrom.Value = null;
this.dtHireTo.Value = null;
```

권장 크기: 130×32. 표시 형식은 `yyyy-MM-dd` 고정 (문화권 무관).

---

## ModernDateRangePicker

기간(From~To) 조회 조건 필드. "DatePicker 2개 + 물결표 라벨" 조합을 컨트롤 하나로
대체한다. 각 필드는 ModernDatePicker와 같은 입력 방식(숫자 8자리 마스크 + 달력
팝업)이고, 오른쪽 프리셋 버튼으로 자주 쓰는 기간을 한 번에 잡는다.

### 속성 / 이벤트

| 멤버 | 타입 | 설명 |
|---|---|---|
| `FromDate` / `ToDate` | `DateTime?` | 기간 시작/종료일. `null` = 미지정 — 한쪽만 지정한 기간도 유효 |
| `SetRange(from, to)` | 메서드 | 두 값을 한 번에 설정 (초기화: `SetRange(null, null)`) |
| `MinDate` / `MaxDate` | `DateTime?` | 두 달력 모두의 선택 가능 범위 |
| `Required` | bool | 필수 입력 표시 — 두 필드 모두 |
| `ShowPresets` | bool | 프리셋 버튼 표시 여부 (기본 true) |
| `RangeChanged` | event | From/To 어느 쪽이든 바뀔 때 1회 발생 |

### 동작 규칙

- **From ≤ To를 컨트롤이 유지한다** — 나중에 바뀐 쪽이 이기고 반대쪽이 같은 값까지
  밀린다(clamp). 폼의 수동 From/To 검증 코드가 필요 없다.
- 프리셋: Today / Yesterday / Last 7 days / Last 30 days / This month(1일~오늘) /
  Last month / Clear. 두 값이 함께 확정되고 `RangeChanged`는 1회만 발생한다.

```csharp
DateTime? from = this.dtHireRange.FromDate;
DateTime? to = this.dtHireRange.ToDate;

if (from.HasValue) { conditions.Add("HIRE_DATE >= '" + from.Value.ToString("yyyy-MM-dd") + "'"); }
if (to.HasValue) { conditions.Add("HIRE_DATE <= '" + to.Value.ToString("yyyy-MM-dd") + "'"); }

// 초기화
this.dtHireRange.SetRange(null, null);
```

권장 크기: 300×32 (프리셋 포함). 샘플: 직원 관리 화면의 입사일 조건.

---

## ModernTimePicker

시각(HH:mm) 입력 필드. `DateTimePicker`의 `Format = Time` 자리를 대체한다.
숫자 4자리 마스크 직접 입력 + 시계 버튼의 30분 간격 목록 팝업(00:00~23:30).

### 속성 / 이벤트

| 멤버 | 타입 | 설명 |
|---|---|---|
| `Value` | `TimeSpan?` | 선택 시각. `null` = 미지정(전체) — 기존 `dtp.Value.TimeOfDay` 읽기가 이 속성으로 바뀐다. 00:00~23:59 밖 할당은 `ArgumentOutOfRangeException`, 초/밀리초는 분으로 절삭 (표시와 값 일치 보장) |
| `PlaceholderText` | string | 입력 전 회색 안내 (기본 `"HH:mm"`) |
| `Required` | bool | 필수 입력 표시 (값이 비어 있는 동안 빨간 점) |
| `ValueChanged` | event | 시각이 바뀔 때 1회 발생 |

```csharp
TimeSpan? sentAfter = this.tpSentAfter.Value;
if (sentAfter.HasValue) { /* SEND_TM 시각 부분 >= sentAfter 필터 */ }
this.tpSentAfter.Value = null;   // 초기화
```

권장 크기: 90×32. 샘플: Logistics & Request의 "After"(발송 시각 하한) 필터.

---

## ModernMonthPicker

년월 선택 필드 (`yyyy-MM`). 정산·마감 등 기준월 조회용. 달력 버튼은 **12개월 그리드
팝업**을 열고 월 클릭이 곧 선택이다. 직접 입력은 숫자 6자리(`202107`) 마스크 방식.

### 속성 / 이벤트

| 멤버 | 타입 | 설명 |
|---|---|---|
| `Value` | `DateTime?` | 선택 년월 — **해당 월 1일로 정규화**. `null` = 미선택(전체) |
| `MinDate` / `MaxDate` | `DateTime?` | 팝업 선택 가능 범위 |
| `PlaceholderText` | string | 입력 전 회색 안내 (기본 `"yyyy-MM"`) |
| `Required` | bool | 필수 입력 표시 (값이 비어 있는 동안 빨간 점) |
| `ValueChanged` | event | 년월이 바뀔 때 1회 발생 |

```csharp
DateTime? month = this.monthHire.Value;
if (month.HasValue) { conditions.Add("HIRE_DATE LIKE '" + month.Value.ToString("yyyy-MM") + "%'"); }
this.monthHire.Value = null;   // 초기화
```

권장 크기: 110×32.

---

## ModernComboBox

단일 선택 콤보. 기본이 **검색형(DropDown)** 이고, 멀티컬럼 드롭다운을 지원한다.
타이핑으로 검색 목록이 열려도 캐럿·선택 영역을 유지하여 다음 글자가 이어서 입력된다.
후보가 없으면 빈 팝업을 닫는다. `DropDown` 이벤트에서 목록을 동기적으로 채우는 방식은 유지하며 열린 목록을 비워도 팝업을 닫는다.
`DropDown` 처리 안에서 목록을 비우고 다시 채우는 동안은 닫기 판단을 미루고, 이벤트가 끝난 최종 목록으로 결정한다.

`AllowedCharacters`(string, 기본 `""`)로 편집 입력·붙여넣기·IME 입력의 허용 문자를 지정한다.
빈 값은 무제한이며 목록에서 선택한 표시값은 변형하지 않는다. `CharacterCasing.Upper`와
`"ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-"`를 함께 쓰면 대문자·숫자·점·하이픈만 입력할 수 있다.
한글 성명 검색에는 제한을 지정하지 않는다.

### 속성·이벤트

| 멤버 | 타입 | 설명 |
|---|---|---|
| `DataSource` | object | 공통 데이터 계약 참고 |
| `DisplayMember` | string | 화면 표시 명칭 컬럼 |
| `ValueMember` | string | 값(코드) 컬럼 |
| `SelectedValue` | object | 선택 값(코드). `DataSource`보다 먼저 설정 가능. 수동 `Items` 컬렉션에서도 동작 — 일치 항목이 없으면 보류했다가 `Items.Add`로 나타나는 시점에 적용 |
| `SelectedItem` | object | 선택 행 (`DataTable` 소스면 `DataRowView`) |
| `SelectedIndex` | int | 미선택 = -1. **-1 할당 = 선택 해제**(플레이스홀더 표시) |
| `Items` | IList | 수동 항목 (`Items.Add("사원")`). `DataSource` 지정 시 무시 |
| `DropDownStyle` | `ComboBoxStyle` | **타이핑 허용 여부**. `DropDown`(기본) = 입력 가능(입력을 모두 지우면 선택 해제), `DropDownList` = 선택 전용. `DropDown`은 WinForms처럼 **셰브런을 눌러야** 목록이 열리고 입력 칸·테두리를 누르면 캐럿만 놓인다. `DropDownList`는 필드 어디를 눌러도 열린다 |
| `AutoCompleteMode` | `AutoCompleteMode` | 기본 `None`(자유 입력). `Suggest` = 목록 좁힘 + 드롭다운(초성 포함), `Append` = 입력 뒤에 **남은 글자를 옅게 겹쳐** 표시(`Tab`/`→`로 확정), `SuggestAppend` = 둘 다. 편집 허용(`DropDownStyle`)과 **별개 축**이라 원본처럼 둘 다 지정한다. 겹쳐 그리기는 텍스트를 바꾸지 않아 **한글 IME 조합 중에도 안전**하다 |
| `AutoCompleteSource` / `AutoCompleteCustomSource` | `AutoCompleteSource` / `AutoCompleteStringCollection` | 값 보관용(레거시 코드 컴파일 호환). 후보는 항상 바인딩된 목록에서 찾는다 |
| `SelectedValue` | `object` | 읽는 시점에 현재 `ValueMember`로 해석한다 — `DataSource`를 먼저 할당하고 `ValueMember`를 나중에 줘도 값이 나온다. 설정은 `DataSource`보다 먼저여도 보류 후 적용 |
| `PlaceholderText` | string | 미선택/미입력 시 힌트 — "전체" 더미 행 대신 사용 권장 |
| `Text` | string | 현재 선택/입력 텍스트 (쓰기는 DropDown 모드만) |
| `Highlight` | bool | 강조 표시 — 필드에 액센트색 테두리를 덧그린다. 한 화면에서 특별히 주목이 필요한 핵심 선택 필드에만 사용 (`Required`와 별개) |
| `SelectedIndexChanged` | 이벤트 | `DataSource` 할당 시 1회 + 사용자 선택 변경 시. 드롭다운에서 **이미 선택된 항목을 다시 골라도 발생**(WinForms `CBN_SELCHANGE` 호환 — 자동 선택된 첫 항목을 그대로 고르는 경우) |
| `DropDown` / `DropDownClosed` | 이벤트 | 드롭다운이 **열리기 직전** / **닫힌 직후** 발생 (WinForms `ComboBox.DropDown` / `DropDownClosed` 호환). 검색형은 타이핑으로 목록이 자동으로 열릴 때도 `DropDown`이 발생 |
| `ItemColorPath` | string | 드롭다운 **항목 글자색**을 담은 컬럼/속성 이름(색 hex). 항목마다 상태를 색으로 구분(예: 채움 상태). 비우면 기본색. 색은 열린 목록 항목에만 적용(닫힌 필드는 기본색) |
| `CharacterCasing` | `CharacterCasing` | 편집 가능 콤보에서 **입력 즉시 대문자/소문자 강제 변환** (`ModernTextBox`와 동일한 축). Lot ID처럼 대문자만 존재하는 코드 검색 콤보에 `Upper`. 기본 `Normal`. `DropDownList`에서는 입력란이 없어 효과 없음 |
| `ConfigureDropDownColumns(...)` | 메서드 | 멀티컬럼 드롭다운 구성 (아래) |

### 예제 — 코드/명칭 콤보 + "전체" 패턴

```csharp
// 서버 부서 테이블: (DEPT_CODE, DEPT_NAME)
this.cboDept.DisplayMember = "DEPT_NAME";
this.cboDept.ValueMember = "DEPT_CODE";
this.cboDept.PlaceholderText = "부서 전체";
this.cboDept.DataSource = deptTable;
this.cboDept.SelectedIndex = -1;            // 미선택 = 전체 (플레이스홀더 표시)

// 조회 시: 미선택이면 null → 조건 생략
string deptCode = this.cboDept.SelectedValue as string;
if (!string.IsNullOrEmpty(deptCode)) { /* DEPT_CODE = 'D3' 조건 추가 */ }
```

### 예제 — 멀티컬럼 드롭다운 (컬럼 수 제한 없음)

`ModernDataGridColumn`을 나열하는 만큼 컬럼이 생긴다. 3개든 4개든 가능:

```csharp
using Modern.Lab.Controls.Wpf.Data;

// 4컬럼 예: 코드 | 부서명 | 사업장 | 사용여부
this.cboDept.ConfigureDropDownColumns(
    new ModernDataGridColumn("DEPT_CODE", "코드", 60),
    new ModernDataGridColumn("DEPT_NAME", "부서명", 110),
    new ModernDataGridColumn("SITE_NAME", "사업장", 90),
    new ModernDataGridColumn("USE_YN", "사용", 40) { TextAlignment = GridTextAlignment.Center });
this.cboDept.DisplayMember = "DEPT_NAME";   // 선택 후 필드에는 명칭만 표시
this.cboDept.ValueMember = "DEPT_CODE";
this.cboDept.DataSource = deptTable;        // 구성 후에 DataSource 할당
```

- 드롭다운에 헤더 행 표시, 폭은 컬럼 합계만큼 자동 확장
- 검색형 모드의 타이핑 필터는 **모든 컬럼 대상** ("D3" 코드로도, "개발"/"ㄱ" 명칭으로도)

---

## ModernCheckComboBox

체크박스 다중 선택 콤보. 미체크 = "전체" 패턴에 최적.

### 속성·이벤트·메서드

| 멤버 | 타입 | 설명 |
|---|---|---|
| `DataSource` / `DisplayMember` / `ValueMember` | | 콤보와 동일 — 화면엔 명칭, 값은 코드 |
| `CheckedValues` | object[] | 체크된 코드 배열. `DataSource`보다 먼저 설정 가능. null 할당 = 전체 해제 |
| `CheckedItems` | object[] | 체크된 원본 행 (읽기 전용) |
| `ItemStyle` | `CheckItemStyle` | `CheckBox`(기본) / `Switch`(온오프 토글) |
| `PlaceholderText` | string | 미체크 시 힌트 (예: "직급 전체") |
| `Text` | string | 체크 항목을 ", "로 연결한 표시 텍스트 (읽기 전용) |
| `CheckedChanged` | 이벤트 | 체크 상태 변경 시 (일괄 변경은 1회) |
| `CheckAll()` / `UncheckAll()` | 메서드 | 전체 체크/해제 — 드롭다운 상단 **"(All)" 헤더**(3상태 헬퍼: 전부=체크/없음=해제/일부=대시, 그리드 필터 팝업과 동일 형식)와 같은 동작. **전체 선택 시 필드는 값을 나열하지 않고 placeholder로 접힌다** — 조회 조건에서 전체 선택 = 빈 선택 = "조건 없음"으로 같은 의미다 |
| `ConfigureDropDownColumns(...)` | 메서드 | **체크 그리드 콤보** — 드롭다운을 멀티컬럼(코드+명칭 등) 행으로 구성. 그리드와 같은 `ModernDataGridColumn` 정의 재사용, 팝업 상단에 헤더 행 표시. 필드 텍스트는 계속 `DisplayMember`. `DataSource` 할당 전에 호출 |

### 예제 — 체크 그리드 콤보 (멀티컬럼 드롭다운)

```csharp
using Modern.Lab.Controls.Wpf.Data;   // ModernDataGridColumn

this.cboEqp.DisplayMember = "EQP_NAME";
this.cboEqp.ValueMember = "EQP_ID";
this.cboEqp.ConfigureDropDownColumns(
    new ModernDataGridColumn("EQP_ID", "Code", 90),
    new ModernDataGridColumn("EQP_NAME", "Equipment", 160),
    new ModernDataGridColumn("STATE", "State", 70));
this.cboEqp.DataSource = eqpTable;     // (EQP_ID, EQP_NAME, STATE)

object[] eqpIds = this.cboEqp.CheckedValues;   // 체크된 설비 코드들
```

### 예제 — 직급 다중 필터 (서버에 코드 전송)

```csharp
// 서버 직급 테이블: (RANK_CODE, RANK_NAME) — 예: (R1, 부장)
this.cboRank.DisplayMember = "RANK_NAME";
this.cboRank.ValueMember = "RANK_CODE";
this.cboRank.PlaceholderText = "직급 전체";
this.cboRank.DataSource = rankTable;

// 조회 시 — 체크된 코드 배열을 그대로 서버 조건으로
object[] rankCodes = this.cboRank.CheckedValues;      // 예: { "R1", "R2" }
if (rankCodes != null && rankCodes.Length > 0)
{
    // RANK_CODE IN ('R1', 'R2') 조건 구성
}

// 프로그램에서 미리 체크해 두기 (DataSource보다 먼저여도 동작)
this.cboRank.CheckedValues = new object[] { "R1", "R2" };

// 초기화
this.cboRank.CheckedValues = null;
```

---

## ModernCheckedListBox

**체크 리스트박스** (`CheckedListBox` 대체) — 체크박스 항목이 **상시 표시**되는
다중 선택 패널 (설비 다중 선택 패널류). `ModernCheckComboBox`의 드롭다운 내용이
필드 없이 화면에 붙박이로 놓인 형태라 API 표면이 같다 — 공간이 좁으면 체크
콤보(드롭다운형), 항목을 항상 보여야 하면 이 컨트롤.

### 속성·이벤트·메서드

| 멤버 | 타입 | 설명 |
|---|---|---|
| `DataSource` / `DisplayMember` / `ValueMember` | | 콤보와 동일 — `Items.Add` 대신 목록을 통째로 바인딩. 재할당 시 체크 초기화 + 보류 값 적용 + `CheckedChanged` 1회 |
| `CheckedValues` | object[] | 체크된 코드 배열. **`DataSource`보다 먼저 설정 가능**(보류 후 적용). null/빈 배열 = 전체 해제 |
| `CheckedItems` | object[] | 체크된 원본 행 (읽기 전용) |
| `ShowSelectAll` | bool | 상단 "(All)" 전체 선택 헤더 (기본 true) — 전부=체크/없음=해제/일부=대시(3상태) |
| `CheckedChanged` | 이벤트 | 체크 상태 변경 시 (일괄 변경은 1회). 항목 클릭 = 즉시 토글(`CheckOnClick=true` 체감) |
| `CheckAll()` / `UncheckAll()` | 메서드 | 전체 체크/해제 |
| `EmptyText` | string | 항목 0개일 때 안내 문구 (기본 `"No data"`, 빈 문자열 = 끔) |

```csharp
this.lstEqp.DisplayMember = "EQP_NAME";
this.lstEqp.ValueMember = "EQP_ID";
this.lstEqp.DataSource = eqpTable;                        // 서버 설비 테이블 그대로
this.lstEqp.CheckedValues = new object[] { "EQP-A01" };   // 초기 체크

// 실행 시 — 체크된 설비 코드 배열을 그대로 서버 조건으로
object[] eqpIds = this.lstEqp.CheckedValues;
```

인덱스 기준 API(`SetItemChecked(i)`)는 없다 — 값 기준(`CheckedValues`)으로
설계되어 정렬/재조회에도 코드가 흔들리지 않는다. 권장 크기: 200×220 이상.

---

## ModernListBox

상시 표시 단일/다중 선택 목록 (`ListBox` 대체). 항목 상태 문법(hover/선택)은
그리드 행과 동일하다. 체크박스가 붙는 다중 선택 판은 `ModernCheckedListBox`.

### 속성·이벤트·메서드

| 멤버 | 설명 |
|---|---|
| `DataSource` / `DisplayMember` / `ValueMember` | 공통 데이터 계약 그대로 — 할당 시 첫 항목 자동 선택 + `SelectedIndexChanged` 1회 |
| `SelectedValue` | 순서 내성 — `DataSource`보다 먼저 설정해도 보류 후 적용. 설정 시 그 항목이 보이도록 자동 스크롤 |
| `SelectedIndex` / `SelectedItem` | 미선택 = -1 / `DataRowView` |
| `MultiSelect` | 기본 false. true면 Shift/Ctrl+클릭 확장 선택 (`SelectionMode = MultiExtended` 대응) |
| `SelectedItems` / `SelectedValues` | 다중 선택 결과 — 목록 표시 순서 배열 |
| `SelectedIndexChanged` | 선택 변경 시 |
| `EmptyText` / `ItemCount` / `ClearSelected()` | 빈 안내 문구 / 항목 수 / 선택 해제 |

```csharp
this.lstEquipment.DisplayMember = "LABEL";
this.lstEquipment.ValueMember = "EQP_ID";
this.lstEquipment.SelectedValue = "EQP-A04";   // 데이터보다 먼저 설정해도 됨
this.lstEquipment.DataSource = replyTable;
```

---

## ModernRadioGroup

배타 선택 라디오 그룹 (`GroupBox`+`RadioButton` 묶음 대체). 코드 테이블을 할당하면
행마다 모던 라디오(원형 + 액센트 점)가 가로(기본)/세로로 나열된다.

### 속성 / 이벤트

| 멤버 | 설명 |
|---|---|
| `DataSource` / `DisplayMember` / `ValueMember` | 공통 데이터 계약과 동일 |
| `SelectedValue` | 선택 값 (`null` = 미선택). `DataSource`보다 먼저 설정 가능 |
| `SelectedValueChanged` | 선택 변경 시 1회 발생 (같은 값 재할당은 미발생) |
| `Vertical` | `true`면 세로 나열 |

```csharp
this.radioSort.DisplayMember = "SORT_NAME";
this.radioSort.ValueMember = "SORT_CODE";
this.radioSort.DataSource = sortTable;          // EMP_NO 사번순 / HIRE_DATE 입사일순 ...
this.radioSort.SelectedValue = "EMP_NO";        // 기본 선택
this.radioSort.SelectedValueChanged += this.OnSortChanged;
```

배타성은 컨트롤이 보장한다 — 개별 라디오 관리 코드가 필요 없다.

---

## ModernTreeView

조직도·분류 계층 선택 트리 (`TreeView` 대체). **평면 자기참조 테이블**(키/부모키/명칭)을
그대로 할당하면 트리가 구성된다 — `TreeNode` 쌓기 코드가 필요 없다.

### 속성 / 이벤트

| 멤버 | 설명 |
|---|---|
| `DataSource` / `DisplayMember` | 공통 데이터 계약과 동일 |
| `IdMember` / `ParentIdMember` | 키/부모 키 컬럼 (부모 없음 = 루트). 순환 데이터(`A→B→A` 등)는 순환을 닫는 노드를 루트로 승격 — 행 유실/멈춤 없음 |
| `SelectedValue` | 선택 노드 키 (`null` = 미선택). 설정 시 조상 자동 펼침 |
| `SelectedItem` | 선택 노드의 원본 행 (읽기 전용) |
| `SelectedValueChanged` | 선택 변경 시 1회 발생 |
| `ForeColorMember` | 노드 텍스트 색 컬럼 (선택). 값은 `"#DC2626"` 같은 색 문자열 — 비었거나 해석 불가면 기본색. Scrap 등 상태 강조용 |
| `IconMember` | 노드 글리프 컬럼 (선택). 값은 프리셋 이름(`Disc`/`Chip`/`Slice`/`Stack`/`Box`/`Folder`/`Dot`, 대소문자 무시) 또는 Segoe MDL2 글리프 16진 코드(`"E950"`). 비었거나 해석 불가 = 아이콘 없음. 종류가 섞인 계보 트리에서 노드 종류를 즉시 구분 |
| `SubTextMember` | 보조 텍스트 컬럼 (선택). 주 텍스트 뒤에 흐린 색으로 표시 — 모델/분류처럼 ID만으로 부족한 문맥 |
| `BadgeMember` / `BadgeColorMember` | 행 오른쪽 끝 상태 배지 (선택). 텍스트 컬럼 + 배경색 컬럼(색 문자열, 글자색 자동 유도 — 그리드 배지와 동일 규칙). 색이 비면 중립 회색 배지, 텍스트가 빈 행은 배지 없음 |
| `BadgeAutoColor` / `BadgeGroupMember` / `BadgeSemanticMember` | 배지 색의 나머지 3경로 — 그리드 Badge 컬럼과 동일 규칙·동일 우선순위(직접 색 > 의미 어휘 > 그룹 키 > 값 자동). `BadgeAutoColor = true`면 색 컬럼 없이 배지 값 자체에서 유도(같은 값 = 항상 같은 색, 겹치는 색 자동 회피), `BadgeGroupMember`는 정수=Palette 인덱스/문자열=FromValue, `BadgeSemanticMember`는 `"Y"`/`"N"`/`"success"` 등 의미 어휘 |
| `BadgeShape` | 상태 배지 모양 — `Rounded`(둥근 사각, 기본) / `Pill`(알약). 값은 `Modern.Lab.Controls.Wpf.Common.ChipShape` |
| `ShowGuideLines` | 들여쓰기 세로 가이드라인 (기본 false). 3단 이상 깊은 계보에서 부모-자식 소속 명확화 |
| `EmptyText` | 노드 0개일 때 가운데 표시할 안내 문구. 기본 `"No data"` — 문맥에 맞게 변경하거나 빈 문자열로 끔. **한국어 화면은 화면에서 지정**한다 |
| `ExpandAll()` / `CollapseAll()` | 전체 펼침/접기 |

```csharp
this.treeOrg.IdMember = "ORG_CODE";
this.treeOrg.ParentIdMember = "PARENT_CODE";
this.treeOrg.DisplayMember = "ORG_NAME";
this.treeOrg.DataSource = orgTable;                    // 서버 조직 테이블 그대로
this.treeOrg.SelectedValueChanged += this.OnOrgTreeSelectionChanged;

// 시각 강화(선택): [글리프] ID  보조텍스트 ....... [상태 배지]
this.treeItem.IconMember = "NODE_ICON";        // 행 값: "Disc"/"Slice" 등 프리셋
this.treeItem.SubTextMember = "SUB_PROD_TYP";
this.treeItem.BadgeMember = "STAT_TYP";
this.treeItem.BadgeColorMember = "NODE_STAT_COLOR";   // 행 값: "#FEE2E2" 등
this.treeItem.ShowGuideLines = true;
```

노드 체크박스·지연 로딩·편집은 미지원(선택 전용). 권장 배치: 그리드 왼쪽 카드, 폭 180~240
(배지/보조 텍스트 사용 시 260~340 권장).

---

## ModernTreeList

**트리 + 컬럼 하이브리드** (DevExpress TreeList 대응) — Lot 계보처럼 계층이 있는
행에 수량·상태 같은 **값 컬럼**을 나란히 붙일 때 쓴다. 트리를 그리드에 평탄화하는
방식이라 컬럼 헤더·폭 조절·행 선택은 `ModernDataGrid`와 같은 문법이고, 데이터는
`ModernTreeView`와 같은 **평면 자기참조 테이블**을 받는다. 계층 없이 목록만
필요하면 그리드를, 값 컬럼 없이 계층 선택만 필요하면 트리를 쓴다.

### 속성 / 이벤트

| 멤버 | 설명 |
|---|---|
| `DataSource` | 평면 자기참조 테이블 (키/부모키/명칭 + 값 컬럼). `DataTable`/`DataView`/`IList`/`IEnumerable` 수용, 부모 없음 = 루트. 순환 데이터는 순환을 닫는 노드를 루트로 승격 — 행 유실/멈춤 없음 |
| `IdMember` / `ParentIdMember` / `DisplayMember` | 키/부모 키/트리 컬럼 텍스트 컬럼 이름 (`ModernTreeView`와 동일) |
| `TreeHeaderText` | 트리 컬럼(첫 컬럼)의 헤더 캡션 (기본 빈 값) |
| `TreeColumnWidth` | 트리 컬럼 픽셀 폭. 0 이하(기본)면 남은 공간 채움(star) — 값 컬럼은 고정 폭, 트리 컬럼이 나머지를 채우는 구성을 권장 |
| `ConfigureColumns(params ModernDataGridColumn[])` | 트리 컬럼 오른쪽에 붙는 값 컬럼 정의 — 그리드와 같은 `ModernDataGridColumn` 문법(`Format`/`TextAlignment`/`TextColor`/`TextSemiBold`/`Width` 적용). **`Kind`는 `Text`/`Badge`만 지원** — 그 외(체크박스/버튼/콤보 등 상호작용 셀)는 Text로 강등된다. **Badge 옵션은 그리드와 전부 같다**(2026-08-29): `BadgeAutoColor`, `BadgeSpinValues`(값 선언형 스핀), `BadgeAccentValues`, `BadgeColorMember`/`BadgeSemanticMember`/`BadgeGroupMember`/`BadgeSpinBorderMember` — 예전에는 `BadgeColorMember`만 트리에 전달됐다. 같은 배지 컬럼의 배지는 **가장 긴 표시값 기준으로 같은 폭**(그리드와 동일 규칙, 데이터 재할당 시 재계산). 폭을 적지 않으면 그 폭이 **컬럼 고정 폭**이 된다(2026-09-07) — 늘어나지 않는다. `BadgeWidthValues`에 올 수 있는 값 목록을 주면 데이터가 아니라 그 어휘로 재므로 조회마다 폭이 흔들리지 않는다 |
| `SelectedValue` | 선택 노드 키 (`null` = 미선택). `DataSource`보다 먼저 설정해도 되고, 설정 시 접힌 조상이 자동 펼쳐지며 그 행으로 스크롤된다 |
| `SelectedItem` | 선택 노드의 원본 행 (읽기 전용, `DataRowView` 등) |
| `SelectedValueChanged` | 선택 변경 시 1회 발생 |
| `ExpandAll()` / `CollapseAll()` | 전체 펼침/접기. **노드는 기본 전부 펼쳐진 상태로 구성**된다 (계보 조회 용도) |
| `EmptyText` | 노드 0개일 때 안내 문구 (기본 `"No data"`, 빈 문자열 = 끔) |
| `ContextMenuStrip` | 노드 우클릭 시 **그 노드를 먼저 선택한 뒤** 지정한 메뉴를 커서 위치에 띄운다 (2026-08-29 추가) — `ModernDataGrid`와 같은 사용법. 폼은 `Opening`에서 `SelectedItem`으로 항목 활성을 정한다. 우클릭한 셀은 현재 셀이 된다. WPF 쪽 `ModernTreeListControl.RowRightClick` 이벤트가 신호다 |
| `ShowCopyMenu` / `CopySelectedRow()` / `CopyCurrentCell()` | 우클릭 메뉴의 **Copy row / Copy cell**(기본 켜짐, 2026-08-29 추가) — 폼 메뉴 끝에 붙거나, 폼 메뉴가 없으면 복사 항목만 있는 메뉴가 뜬다. Copy row = 트리 텍스트 + 값 컬럼 탭 구분(`Format` 적용), Copy cell = 현재 셀. Ctrl+C / Ctrl+Shift+C 단축키 동일 — 그리드와 같은 규칙 |
| `ShowStatusBar` / `StatusText` / `StatusCountFormat` | 하단 상태바 (2026-09-17 추가, 기본 꺼짐 — 그리드와 같은 기본값). 왼쪽은 **보이는 노드 수**(`StatusCountFormat`, 기본 `"{0:N0} rows"`)로, 접힌 자식은 세지 않는다 — 펼치고 접을 때마다 다시 쓴다. 오른쪽은 `StatusText` 자유 문구 |
| `RowColorMember` | 행 배경색으로 쓸 컬럼 이름 (2026-09-17 추가, 기본 빈 값 = 끔). `"#FEE2E2"` 같은 색 문자열이나 색 이름이며 해석 불가하면 기본 배경이다. **선택·hover 가 이 색보다 우선**한다 — 그리드와 같은 우선순위 |
| `AllowColumnFilters` | 값 컬럼 헤더의 깔때기 (2026-09-17 추가, 기본 켬 — 그리드와 같다). 고른 값에 걸린 노드와 **그 조상**이 남아 계보가 끊기지 않고, 거르는 동안 남은 가지는 접힘과 무관하게 보인다. 트리 컬럼에는 붙지 않는다(계보 축이라 값으로 거를 대상이 아니다). `HasActiveColumnFilters` / `ClearColumnFilters()`로 상태를 읽고 푼다 |
| `AllowFindPanel` / `ShowFindPanel()` | Ctrl+F 찾기 (2026-09-17 추가, 기본 켬). 노드를 거르지 않고 일치 셀로 이동한다 — 대상은 **보이는 노드**의 트리 텍스트와 값 컬럼이라, 전체를 찾으려면 `ExpandAll()`을 먼저 부른다. Esc 닫기 · F3/Shift+F3 다음·이전 |
| `ShowExportMenu` / `ExportSheetName` / `ExportFileNamePrefix` / `ExportXlsx(...)` / `ExportXlsxInteractive()` | 우클릭 "Export to Excel..." (2026-09-17 추가, 기본 켬 — `ConfigureColumns`를 부른 트리에만 항목이 뜬다). 엑셀에는 **트리 컬럼이 첫 컬럼**으로(헤더 `TreeHeaderText`, 값 `DisplayMember`) 그 뒤에 값 컬럼이 선언 순서대로 실린다. 들여쓰기는 나가지 않는다 — 계보는 부모 키 컬럼이 말한다. `IExcelExportGrid`를 구현하므로 폼의 공통 내보내기 헬퍼에 그대로 넘길 수 있다 |
| (현재 셀 언더라인) | 클릭한 셀 아래 옅은 액센트 2px 언더라인 — 그리드와 같은 시각 언어 (2026-08-29 추가) |

```csharp
using Modern.Lab.Controls.Wpf.Data; // ModernDataGridColumn

this.treeLot.IdMember = "ITEM_ID";
this.treeLot.ParentIdMember = "PARENT_LOT_ID";
this.treeLot.DisplayMember = "ITEM_ID";
this.treeLot.TreeHeaderText = "Lot";
this.treeLot.ConfigureColumns(
    new ModernDataGridColumn("QTY", "Qty", 70) { TextAlignment = GridTextAlignment.Right, Format = "N0" },
    new ModernDataGridColumn("STAT", "Status", 90)
    {
        Kind = GridColumnKind.Badge,
        BadgeColorMember = "STAT_COLOR",
        TextAlignment = GridTextAlignment.Center
    });
this.treeLot.DataSource = genealogyTable;   // 서버 계보 테이블 그대로
this.treeLot.SelectedValueChanged += this.OnLotTreeSelectionChanged;
```

정렬·컬럼 값 필터·Ctrl+F 찾기·자동 폭·엑셀 내보내기는 미지원(트리는 계층 순서가 곧
의미) — 그 기능들이 필요한 목록은 `ModernDataGrid`가 맞다. 셀 편집도 미지원(조회 전용).
권장 크기: 480×300 이상, `Dock`/`Anchor`로 영역 채움.

---

## ModernDataGrid

명시적 컬럼의 데이터 소스 대입당 자동 폭 계산은 한 번이다. 배지·행별 버튼의 통일 최소폭은
AutoFit을 꺼도 유지하며, 화면에 없는 행의 값도 최소폭 계산에 포함한다.
배지·행별 버튼의 폭 계산은 호출 안에서 동일 표시 문자열을 한 번만 실측한다(저장 한도 이내).
새로운 값은 한도 이후에도 실측하며 다음 호출과 측정 결과를 공유하지 않는다.

래퍼 `Dispose` 시 외부 데이터 구독·크기 변경 타이머·컬럼 감시를 정리한다. 외부 표를 계속 보관해도
닫힌 래퍼와 WPF 자식이 그 구독에 남지 않는다. `ICollectionView.Filter`에 직접 설치한 조건자도 소스 교체·폐기 때
해제하되 다른 소비자의 대체 조건자는 보존한다. 순수 WPF의 임시 언로드는 별도이며 재사용할 수 있다.
외부 뷰가 행 편집·추가 중이면 편집을 취소하거나 금지된 Filter 갱신을 하지 않고, 조건자에서 그리드 참조만
끊어 다음 뷰 갱신부터 모든 행을 허용한다. 폐기 중 새 소스 대입으로 구독이 복구되지 않는다.
`DataTable`·`DataView`는 `CustomFilter`가 외부 `RowFilter`를 변경하는 별도 경로이며 소스 교체·폐기 때
해제·복원하지 않는다. 같은 뷰의 다른 소비자에게도 적용되므로 격리가 필요하면 전용 `new DataView(table)`을
바인딩한다. 자세한 경계는 [교체 가이드](migration/ModernDataGrid.md#데이터-소스와-폐기)를 따른다.

데이터 그리드 — 기본은 읽기 전용 조회용이고, `ReadOnly = false`로 두면 텍스트
컬럼 셀 편집이 켜진다(아래 "셀 편집" 절). 헤더 클릭 정렬, 컬럼 폭 마우스 조절,
행 마우스 hover 배경, 현재 셀 옅은 액센트 언더라인 지원. 텍스트 컬럼은
값이 컬럼 폭보다 길면 `…`로 잘리고, **잘렸을 때만** 호버 시 전체 값 툴팁이 뜬다
(안 잘리면 툴팁 없음). 툴팁은 공용 모던 스타일(둥근 Surface 카드 + 소프트 그림자,
테마 토큰 색)을 따른다.

### 속성·이벤트·메서드

| 멤버 | 타입 | 설명 |
|---|---|---|
| `DataSource` | object | 할당 시 첫 행 자동 선택 + `SelectionChanged` 1회 |
| `ConfigureColumns(...)` | 메서드 | 명시적 컬럼 정의 — 개수 제한 없음. `DataSource` 할당 전에 호출 |
| `ColumnDefinitions` | `ModernDataGridColumn[]` | `ConfigureColumns`로 선언한 정의의 복사본 — 화면과 동일한 컬럼 구성으로 커스텀 파생 출력을 만들 때 단일 원천으로 사용. 엑셀 저장은 `ExportXlsx`가 이미 해준다 |
| `ExportXlsx(path, sheetName, data)` | 메서드 | 화면 컬럼 정의 그대로(순서·캡션·`Format`) 데이터 전체를 진짜 .xlsx로 저장 — 외부 라이브러리 없음. CheckBox/Button 컬럼은 자동 제외. `data`는 그리드 `DataSource`가 아니라 인자 — 페이지 화면에서도 전체 결과 저장. `ConfigureColumns` 선언 후에만 사용 가능. **저장 대화상자·파일명·성공/실패 알림까지 필요하면 폼에서 `ModernFormBase.ExportToExcel(...)`을 부른다**(이 메서드를 내부에서 호출한다) |
| `AutoGenerateColumns` | bool | 기본 true. `ConfigureColumns` 호출 시 자동 false |
| `RowCount` | int | 현재 행 수 — 조회 건수 표시 연동 |
| `SelectedItem` | object | 선택 행 (`DataRowView`) — 기존 `CurrentRow.DataBoundItem` 대체. 프로그램으로 설정하면 그 행이 보이도록 자동 스크롤 |
| `SelectedIndex` | int | 미선택 = -1. 프로그램으로 설정하면 그 행이 보이도록 자동 스크롤 |
| `SelectionChanged` | 이벤트 | 행 선택 변경 시 |
| `MultiSelect` | bool | **기본 false(단일 선택)**. true면 Shift+클릭(범위)/Ctrl+클릭(개별)로 여러 행 선택 — 결과는 `SelectedItems`, Ctrl+C는 선택 행 전부 복사. `SelectedItem`/`SelectedIndex`는 현재 행 의미 유지 |
| `SelectedItems` | object[] | 선택된 행들의 복사본 — **현재 뷰 순서**(정렬/필터 반영), `DataTable` 소스면 `DataRowView`. 단일 선택 그리드에서는 0~1개 |
| `FrozenColumnCount` | int | 가로 스크롤 시 왼쪽에 고정할 컬럼 수 (기본 0). 명시적 컬럼 정의 화면은 컬럼 정의의 `Frozen = true`를 권장 — `ConfigureColumns`가 이 값을 자동 설정한다. 샘플: Grid Editing (Sample Id 고정) |
| `SelectAll()` | 메서드 | 모든 행 선택 — `MultiSelect = true`일 때만 동작 (Ctrl+A/우클릭 `Select all`과 동일) |
| `ClearColumnFilters()` / `HasActiveColumnFilters` | 메서드/bool | 모든 컬럼 값 필터 일괄 해제 / 활성 필터 유무 — 우클릭 `Clear filters`(필터가 걸려 있을 때만 표시)와 동일, Reset 버튼 연동용 |
| (우클릭 컬럼 고정) | 메뉴 | 우클릭은 클릭한 셀을 현재 셀로 갱신(언더라인 이동)하고, `More ▸` 아래 `Freeze columns up to here`가 그 컬럼까지 왼쪽 고정, `Unfreeze columns`(고정이 있을 때만)가 해제한다 — 사용자가 런타임에 조절, 저장은 안 됨(화면 재오픈 시 폼 설정) |
| `ShowStatusBar` | bool | 기본 false. true면 그리드 하단에 상태바 표시 — 왼쪽에 행 수 자동 표기 |
| `StatusCountFormat` | string | 상태바 행 수 형식. 기본 `"{0:N0} rows"`(ModernPagination 표기와 동일 문형) — `{0}`에 현재 행 수. 화면 문맥에 맞게 `"{0:N0} lots"` 등으로 바꾼다 |
| `StatusText` | string | 상태바 오른쪽 자유 텍스트 (선택 대상·조회 조건 등) |
| `AlternatingRowColors` | bool | 기본 false. true면 홀수 행이 테마 교차색(`Brush.GridRowAlt`)으로 칠해진다 — 행이 많고 가로로 긴 그리드에서 시선 유지용. `ModernSpreadGrid`에도 동명 속성이 있다(그쪽은 기존 화면 보존을 위해 기본 true) |
| `RowColorSelector` | Func<object, string> | 보조 컬럼 없이 행 객체에서 배경색을 계산. 선택·hover 다음, RowColorMember보다 우선. 빈 값·잘못된 색·예외는 기존 컬럼 색 또는 기본 배경으로 폴백. 순수 계산 함수만 지정하고 외부 상태를 바꾼 뒤에는 새 함수로 지정해 갱신 |
| `RowMarkerSelector` | Func<object, string> | 행 객체에서 짧은 모서리 표식 문자열을 계산. 첫 번째 보이는 열의 왼쪽 위에 8 DIP로 표시하며 행 높이·열 너비·클릭 영역을 바꾸지 않는다. 빈 값·예외는 숨김. 테마의 SuccessText 색을 쓰고 처음 나타날 때 1.4초 동안만 밝기를 변화시킨 뒤 고정한다. Windows 애니메이션 끄기를 존중한다. 외부 상태 변경 후 새 함수로 지정해 갱신하며 데이터 소스 또는 함수 교체 시 최초 강조 기준을 초기화한다 |
| `SelectRow(predicate)` | bool | 현재 정렬·필터 뷰에서 조건을 만족하는 첫 행을 선택하고 스크롤. 성공 true, 없거나 null 조건이면 false와 기존 선택 유지. 행 객체는 DataTable/DataView에서 DataRowView이며 조회·쓰기 기능은 없음 |
| `RowColorMember` | string | 행 배경색 컬럼 (선택). 값은 `"#FEE2E2"` 같은 색 문자열 — 비었거나 해석 불가한 행은 기본 배경 유지. 상태별 행 강조용 (상태 표시가 한 컬럼으로 충분하면 `Kind = Badge` 컬럼도 대안) |
| `RowForegroundMember` | string | 행 **글자색** 컬럼 (선택, 2026-08-29). 값은 `"#DC2626"` 같은 색 문자열 — 비었거나 해석 불가한 행은 기본 글자색. 판정 FAIL 행 전체를 빨간 글자로 보이게 하는 자리. 배지/링크/버튼 셀은 자기 색을 유지하고, 선택 셀의 글자색이 우선한다 |
| `RowKeyMember` / `RestoreSelectionByIndex` | string / bool | **재바인딩 시 선택·스크롤 복원** (2026-08-09 추가). 행을 알아보는 컬럼을 주면 `DataSource` 재할당 때 **보던 행과 보던 구간을 되찾는다** — 재조회마다 첫 행으로 튀지 않는다. 여러 컬럼이 있어야 행이 유일해지면 **콤마로 나열**(`"EQP_ID,PORT_SLOT"`). 비우면(기본) 기존 동작 그대로. 새 데이터에 그 키가 없으면 첫 행으로 떨어지고, `RestoreSelectionByIndex = true`면 **같은 행 번호**를 지킨다(처리된 항목이 목록에서 빠지는 화면용). 스크롤 위치도 함께 되돌리되, 그 행이 멀리 이동해 **선택이 화면 밖이 되면 선택 쪽으로 맞춘다** (어디를 보고 있었는지보다 무엇이 선택돼 있는지가 중요하다). `SelectionChanged`는 여전히 할당당 1회다. 계약: `docs/state-contracts/grid-selection-restore.md` |
| `EmptyText` | string | 데이터 0건일 때 데이터 영역 가운데 표시할 안내 문구. 기본 `"No data"` — 화면 문맥에 맞게 변경(`"Search by part number"` 등)하거나 빈 문자열로 끔. **한국어 화면은 화면에서 지정**한다 |
| `CellButtonClick` | 이벤트 | 버튼 컬럼(`Kind = Button`) 셀 클릭 시. `e.Item`이 클릭 행(`DataRowView`), `e.DataPropertyName`이 버튼 컬럼 이름 |
| `CellLinkClick` | 이벤트 | 링크 컬럼(`Kind = Link`) 셀 클릭 시 — 클릭한 행이 먼저 현재 행으로 선택된 뒤 발생. `e.Item`(행) + `e.DataPropertyName`(링크 컬럼 이름). "Lot ID 클릭 → 이력 화면" 관례용 |
| `RowDoubleClick` | 이벤트 | 행 왼쪽 더블클릭 시 — 그 행이 먼저 현재 행으로 선택된 뒤 발생(2026-08-29 추가). `SelectedItem`을 결정 패널 등으로 넘기는 관례용. 헤더·빈 영역은 제외 |
| 링크 컬럼 | 컬럼 정의 | `Kind = GridColumnKind.Link` — 값 텍스트가 액센트색 링크(**항상 밑줄**, 호버 시 진한 액센트)로 그려진다. 정렬/값 필터/복사/자동 폭은 텍스트 컬럼과 동일. 값이 빈 행은 빈 셀 |
| `CheckStyle` / `CheckEnabledMember` | 컬럼 정의 | **CheckBox 컬럼 전용 옵션.** `CheckStyle = GridCheckStyle.Switch`면 네모 체크박스 대신 **온/오프 스위치**(ModernCheckBox Switch와 같은 문법)로 그린다 — 행을 고르는 컬럼이 아니라 **행마다의 설정을 켜고 끄는** 컬럼용이다. `CheckEnabledMember`는 행별 토글 가능 여부 컬럼(bool 또는 `"Y"`/`"true"`/`"1"`) — 거짓인 행은 잠긴 모양이 되고 클릭해도 값이 바뀌지 않는다(조건이 맞을 때만 바꿀 수 있는 설정). 스위치는 `HeaderCheckBox`(전체 선택)와 함께 쓰지 않는다 |
| `CheckTrueValue` / `CheckFalseValue` | 컬럼 정의 | **CheckBox 컬럼의 값 어휘** (2026-08-11 추가). 값 컬럼이 bool이 아니라 `"Y"`/`"N"` 같은 문자열일 때 그 어휘를 선언하면 **조회가 이미 내려주는 컬럼에 체크 상태를 그대로 물릴 수 있다** — 화면용 bool 컬럼을 따로 만들 필요가 없다. 표시는 어휘 또는 일반 참 규칙(`"Y"`/`"YES"`/`"TRUE"`/`"1"`)으로 읽고, 토글과 헤더 전체 선택은 **원본 어휘 그대로** 되쓴다(체크 → `CheckTrueValue`). 널/빈 값/모르는 값은 해제(예외 없음). **둘 다 채워야** 켜진다 — 한쪽만 채우면 끈 자리에 쓸 값이 정해지지 않아 bool 컬럼으로 취급한다. 비워 두면 지금까지와 같은 bool 컬럼. WinForms `DataGridViewCheckBoxColumn.TrueValue`/`FalseValue`와 같은 자리. 예: `new ModernDataGridColumn("USE_YN", "Use", 60d) { Kind = GridColumnKind.CheckBox, CheckTrueValue = "Y", CheckFalseValue = "N" }` |
| `CellCheckChanged` | 이벤트 | 체크박스 컬럼(`Kind = CheckBox`) 값이 **클릭으로** 바뀔 때. 토글 직후 행 편집을 즉시 확정한다(`IEditableObject.EndEdit`) — `DataRowView`는 값 쓰기로 편집 상태에 들어가는데, 읽기 전용 그리드는 그 편집을 스스로 끝내 주지 않아 열린 채 남으면 다음 행을 고를 때 `DataView`가 행을 다시 자리잡으며 목록이 출렁인다. `e.Item`(행) + `e.DataPropertyName`(컬럼) + `e.IsChecked`(변경 후 값). 체크박스는 원본 행 값을 이미 양방향으로 갱신하므로 "선택 표시"만 필요하면 구독하지 않아도 된다 — 토글이 **곧바로 처리로 이어져야 할 때**(설정 저장, 서버 반영) 쓴다. 헤더 체크박스로 일괄 변경하면 **값이 실제로 바뀐 행마다** 한 번씩 발생한다(같은 값이던 행은 발생하지 않는다). 서버 호출이 실패하면 화면 값을 되돌리는 것은 화면 몫이다(`row["AUTO"] = !e.IsChecked`) |
| 콤보 컬럼 | 컬럼 정의 | `Kind = GridColumnKind.Combo` — 셀 콤보로 `ComboItems`(고정 선택지 `string[]`) 중 하나를 고르면 원본 행 컬럼 값이 즉시 갱신된다(양방향, 판정/등급 입력용). `ComboEnabledMember` 컬럼 값(bool/`"Y"`)으로 행별 입력 가능 제어 — 비활성 행은 회색 잠금. `ComboItemColors`(선택지와 같은 순서의 색 배열)를 주면 선택 값/드롭다운 항목이 레티클(둥근 사각) 배지로 표시되고 필드 표면도 선택 값의 배지 색으로 칠해진다. 입력분만 전송하려면 바인딩 직전 `AcceptChanges()` 후 `GetChanges()` 사용 |
| `CopySelectedRow()` | 메서드 | 선택 행을 탭 구분 텍스트로 클립보드에 복사(`Ctrl+C`와 동일) — 다중 선택이면 선택 행 전부가 뷰 순서대로 여러 줄. 체크박스·버튼·스피너 컬럼은 제외 |
| `CopyCurrentCell()` | 메서드 | 현재 셀 하나만 복사(`Ctrl+Shift+C`와 동일) |
| `ShowCopyMenu` | bool | 우클릭 메뉴에 복사 항목(`Copy row`/`Copy cell`, 단축키 표기)을 넣을지 여부. 기본 `true` — `ContextMenuStrip`이 있으면 **그 메뉴 맨 끝에** 구분선과 함께 붙는다(반복 우클릭에도 중복되지 않고, 메뉴를 공유해도 우클릭한 그리드가 대상). 그리드 `Dispose`/메뉴 교체 시 주입 항목은 자동으로 걷힌다. **공통 항목의 모양**(2026-08-29): 최상위는 `Copy row`·`Copy cell`뿐이고 `Find...`·`Export to Excel...`·`Select all`·`Clear filters`·`Freeze/Unfreeze columns`는 **`More ▸` 하위 메뉴**에 접힌다 — 폼 액션이 몇 개뿐인 작은 표에서 메뉴가 8~9줄로 늘어나지 않게. 하위 항목이 없으면 `More`도 없다 |
| `ShowExportMenu` / `ExportSheetName` / `ExportFileNamePrefix` | bool / string / string | 우클릭 메뉴 **"Export to Excel..."** 항목 (2026-08-02 추가, 기본 true — `ConfigureColumns` 그리드만 표시). 저장 대화상자를 띄워 그 그리드를 .xlsx로 저장한다. 데이터는 `ResolveExportData()` 규칙(전체 결과 우선 — 아래 행)이라 페이지 화면도 전체가 저장된다. 시트 이름(빈 값=Data)·파일명 접두사(빈 값=Export, 뒤에 `_yyyyMMdd_HHmmss`)는 디자이너에서 지정한다. 성공은 조용히, 실패만 메시지 박스 |
| `ExportXlsxInteractive()` / `ResolveExportData()` | 메서드 | 우클릭 내보내기와 동일 동작을 코드에서 호출/저장 데이터 해석 규칙 — ① `FilterValueSource`(조회 결과 전체) ② `DataTable` 소스 ③ `DataView` 변환 ④ `DataRow` 목록 스키마 복제. 화면별 파일명·성공 토스트가 필요하면 `ModernFormBase.ExportToExcel` 버튼 경로를 쓴다 |
| `ContextMenuStrip` | 속성(표준) | 지정하면 **행 우클릭 시 그 행을 먼저 현재 행으로 선택**한 뒤 메뉴가 커서 위치에 뜬다 — 메뉴 핸들러는 `SelectedItem` 기준으로 처리. 행 밖(헤더/빈 영역) 우클릭에는 뜨지 않는다. 행 단위 부가 처리가 많아 버튼 컬럼으로 다 담기 어려울 때 사용 |
| `EnableColumnVirtualization` | bool | **기본 false.** WPF DataGrid의 같은 이름 속성을 그대로 노출한다. 켜면 가로 뷰포트 밖 컬럼의 셀을 만들지 않아 컬럼이 많은 표의 첫 표시와 세로 스크롤이 빨라진다(실측 64컬럼·2000행, `ConfigureColumns` + `AutoFitColumns`: 첫 표시 1279→479ms, 세로 스크롤 496→162ms). 대신 가로로 크게 옮길 때 새 컬럼의 셀을 그때 만들어 200ms 안팎이 든다. **컬럼이 가로로 넘치고 폭이 절대값일 때 효과가 있다** — `AutoFitColumns`를 켠 표(자동 생성 컬럼 포함)가 그 조건이다. 폭 지정도 `AutoFitColumns`도 없으면 기본 폭이 `*`라 컬럼이 화면에 다 들어가 켜도 차이가 없다. 세로 목록 위주의 넓은 읽기 전용 표에서 화면 단위로 켜고, 켠 화면은 셀 병합·고정 컬럼 표시를 확인한다 |
| `AllowColumnFilters` | bool | 기본 true. 켜져 있으면 텍스트/배지 컬럼 헤더에 **깔때기 버튼**이 붙고, 클릭 시 그 컬럼의 고유 값 체크리스트 팝업으로 행을 거른다(엑셀식 값 필터 — 여러 값을 체크한 뒤 **Apply로 확정**, `Clear` = 필터 즉시 해제, ✕/바깥 클릭 = 변경 취소). 팝업 검색창은 한글 초성 매칭(`ㄱㅁㅅ` → `김민수`)으로 **체크리스트를 좁히면서 선택도 그 결과로 맞춘다** — 검색에 맞는 값만 체크되므로 그대로 `Apply` 하면 그 값들로 걸러지고, 검색어를 지우면 다시 전체 선택으로 돌아간다(별도 제안 드롭다운 없음). 필터는 화면 뷰에만 적용되고 원본 표의 행은 지우지 않는다 — 다만 `DataTable`/`DataView` 소스에서 그 "화면 뷰"는 **공유 `DefaultView`**라 같은 뷰를 보는 다른 소비자에게도 남는다(위 "데이터 소스와 폐기" 경계). 필터가 걸린 컬럼은 깔때기가 액센트색으로 표시된다. 선택 상태는 `DataSource` 재할당(재조회) 후에도 유지된다. 상태바 행 수/EmptyText는 필터 결과를 따라간다 |
| `AllowFindPanel` | bool | 기본 true. **Ctrl+F 찾기 — 엑셀식 소유된 모덜리스 찾기 창** (필터가 아니다). Ctrl+F(또는 우클릭 메뉴 `Find...`)로 그리드 오른쪽 위에 찾기 창이 열린다 — 표준 찾기 대화상자 동작: 호스트 창 위에만 항상 표시(다른 앱 위로는 안 감), 호스트 최소화 동행, 작업표시줄/Alt-Tab 미표시, 제목줄 드래그로 이동(위치는 세션 동안 유지), 열린 채 Ctrl+F 재입력 = 검색창 재포커스, **그리드가 화면에서 숨겨지면(셸/탭/MDI에서 다른 메뉴로 전환) 찾기 창도 함께 내려간다**(2026-08-06 — 소유자인 최상위 창은 그대로 활성이라 스스로는 안 사라지므로 그리드 가시성에 연동; 2026-08-07 — 그리드 자신이 아니라 **조상 컨테이너만 숨는 경우**도 래퍼가 조상 체인 가시성을 구독해 내린다; 2026-08-25 — 화면을 숨기지 않고 **다른 화면을 앞으로 가져와 덮는 전환**(셸 홈 버튼 등 z-순서 교체)도 완전 가림 실측으로 내린다). **Find Next**(검색창 Enter/F3) = 다음 일치 셀로 선택·스크롤 이동(끝나면 처음부터), **Shift+F3/Shift+Enter** = 이전 일치, **Find All** = 일치 목록(`Row n · 컬럼 · 값`) 나열 + 클릭 시 그 셀로 이동. 화면에 보이는 일치 텍스트는 노란 배경으로 강조(가상화 행에도 자동 적용). 대상은 텍스트를 담은 컬럼(Text/Link/Badge), 화면 표시 텍스트 기준 부분 일치·대소문자 무시. Esc/✕로 닫으면 강조 해제. 행을 거르지 않으므로 페이징 연동 코드 불필요(검색 범위는 바인딩된 데이터 = 현재 페이지) |
| `ShowFindPanel()` / `HideFindPanel()` | 메서드 | 찾기 창 열기(검색어 입력에 포커스, 열려 있으면 활성화)/닫기(강조 해제) — 화면에 자체 찾기 버튼을 둘 때 사용. **Ctrl+F 키는 그리드에 포커스가 있을 때 처리된다** — 조회 조건·버튼에 포커스가 있어도 열리게 하려면 폼 `ProcessCmdKey`에서 받아 `ShowFindPanel()`을 부른다 (Samples `ModernFormBase.RegisterFindShortcut` 참고) |
| `IsFindPanelOpen` | bool(읽기) | 찾기 창 열림 상태 — 자체 검사·토글 UI 판정용 (2026-08-02 추가) |
| `ReadOnly` | bool | **기본 true(잠김)** — false면 텍스트 컬럼 셀 편집이 켜진다. 아래 "셀 편집" 절 참고 |
| `CellValueChanged` / `CellValidating` | 이벤트 | 셀 편집 커밋 알림/커밋 직전 검증 (편집 그리드 전용) — 아래 "셀 편집" 절 |
| `EndEdit()` / `CancelEdit()` | 메서드 | 열려 있는 셀/행 편집 확정/취소 — 저장 버튼 처리 첫 줄에서 `EndEdit()` |
| `AddRow(...)` / `RemoveSelectedRow()` / `RemoveRow(item)` | 메서드 | 행 추가/삭제 (편집 화면용) — 아래 "셀 편집" 절 |

### 셀 편집 (ReadOnly = false)

기본은 읽기 전용이다. `ReadOnly = false`로 두면 **텍스트 컬럼**을
더블클릭/F2/타이핑으로 편집한다 — Enter = 확정 후 아래 셀, Tab = 오른쪽 셀,
Esc = 취소. 체크박스/콤보 컬럼은 이 값과 무관하게 항상 양방향이고,
배지/버튼/스피너/링크 컬럼은 항상 잠김이다.

- **컬럼 잠금**: 키 컬럼처럼 값이 바뀌면 안 되는 컬럼은 정의에
  `ReadOnly = true`를 준다.
- **숫자/날짜는 표시 서식 그대로 편집** — `Format = "N0"` 컬럼은 편집기에
  `1,234`가 보이고 콤마째 고쳐도 숫자로 되돌려 커밋한다. 타입에 안 맞는
  입력(숫자 컬럼에 문자 등)은 **빨간 테두리 + 커밋 거부**로 그리드가 스스로
  막는다 — 폼 검증 코드가 필요 없다.
- **`CellValidating`**: 커밋 직전 발생 — `e.NewText`를 보고 `e.Cancel = true`면
  커밋이 취소되고 셀이 편집 상태로 남는다. 값 범위·중복 같은 업무 규칙 검증용.
- **`CellValueChanged`**: 값이 **실제로 바뀐** 커밋에만 발생 —
  `e.Item`/`e.DataPropertyName`/`e.OldValue`/`e.NewValue`(원본 컬럼 타입 값).
- 셀 커밋 즉시 행 편집까지 확정되어 DataTable에 반영된다 — 저장은
  "`EndEdit()` → `GetChanges()` 전송 → `AcceptChanges()`" 패턴 그대로.
- 편집 중 표시: 행 하이라이트(선택색)는 그대로 유지되고, **편집 중인 셀만
  흰 표면 + 액센트색 2px 테두리 프레임**으로 구분된다 (엑셀의 편집 셀 문법).

```csharp
this.gridSamples.ReadOnly = false;   // 편집 켜기 (기본은 잠김)

this.gridSamples.ConfigureColumns(
    new ModernDataGridColumn("SAMPLE_ID", "Sample Id", 110) { ReadOnly = true },  // 키 컬럼 잠금
    new ModernDataGridColumn("QTY", "Qty", 80) { Format = "N0", TextAlignment = GridTextAlignment.Right },
    new ModernDataGridColumn("TEST_DATE", "Test Date", 110) { Format = "yyyy-MM-dd" });

// 업무 규칙 검증 — 타입 오류는 그리드가 알아서 막으므로 범위만 본다
this.gridSamples.CellValidating += (s, e) =>
{
    if (e.DataPropertyName == "QTY" && int.Parse(e.NewText, NumberStyles.Number) < 0)
    {
        e.Cancel = true;   // 커밋 취소 — 셀이 편집 상태로 남는다
    }
};

// 행 추가/삭제 — 값 순서는 ConfigureColumns의 데이터 컬럼 선언 순서
this.gridSamples.AddRow("SMP-0006", 0, DateTime.Today);
this.gridSamples.RemoveSelectedRow();

// 저장 — 열려 있는 편집을 확정한 뒤 변경분만 전송
this.gridSamples.EndEdit();
DataTable changes = this.samples.GetChanges();
if (changes != null) { this.SendToServer(changes); this.samples.AcceptChanges(); }
```

### 예제 — 컬럼 정의와 선택 행 사용

```csharp
using Modern.Lab.Controls.Wpf.Data;

// 폼 로드 시 1회
this.gridEmployee.ConfigureColumns(
    new ModernDataGridColumn("EMP_NO", "사번", 90),
    new ModernDataGridColumn("EMP_NAME", "이름", 110),
    new ModernDataGridColumn("HIRE_DATE", "입사일", 110) { TextAlignment = GridTextAlignment.Center },
    new ModernDataGridColumn("EMAIL", "이메일"));          // 폭 생략 = 남은 공간 채움

// 조회 후
this.gridEmployee.DataSource = replyTable;
this.lblCount.Text = "조회 " + this.gridEmployee.RowCount + "건";

// 선택 행 값 읽기
DataRowView row = this.gridEmployee.SelectedItem as DataRowView;
if (row != null)
{
    string empNo = row["EMP_NO"].ToString();
}

// 상태바 — 행 수는 자동, 오른쪽 텍스트는 조회 시마다 갱신
this.gridEmployee.ShowStatusBar = true;
this.gridEmployee.StatusCountFormat = "조회 {0:N0}건";
this.gridEmployee.StatusText = "부서: 개발1팀";   // 비워두면 표시 없음
```

### 컬럼 캡션 용어사전 (선택 기능)

"필드 이름 → 표준 캡션" 용어집. **기본 노선은 "하드코딩 사전 + 자동 변환
폴백"이다** (2026-08-06 확정 — 용어 테이블 신설·적재마다 결재가 필요한 환경
기준): 검수한 용어만 `GridCaptionDictionary`에 하드코딩하고, 사전에 없는
컬럼은 폴백(`GridCaptionCatalog.Humanize`)이 `EQP_ID` → "Eqp Id"로 변환한다.
용어 수정은 일반 배포에 실려 나가고, 커밋 diff가 곧 검수 기록이 된다.

**DB 덮어쓰기는 선택 노선이다** — DB로 용어를 관리할 수 있는 환경이면
아래 `UI_TERM_DIC` 방식으로 시드 위에 덮어쓴다 (용어 수정 = DB UPDATE +
앱 재시작, 컨트롤 DLL 재배포 없음). 역할 분담:

| 구성 요소 | 소재 | 역할 |
|---|---|---|
| `GridCaptionCatalog` | 컨트롤 DLL (`Modern.Lab.Controls.Wpf.Data`) | 등록형 그릇 — 캡션 인자 없는 컬럼 정의가 여기서 캡션을 받는다 |
| `GridCaptionDictionary` | **회사 상위 Common 소스** — `Modern.Lab.Hosting/Dictionaries/` | 폴백 시드(오프라인/DB 장애용) + DB 조회 결과를 그릇에 붓는 `Apply(DataTable, …)` 로더 |
| `UI_TERM_DIC` 테이블 | DB | 운영 용어 원본 — 영문/한글 두 열, `TERM_TYP`으로 그리드('GRID')/맵('MAP') 용어를 함께 관리 |

```sql
CREATE TABLE UI_TERM_DIC (
    TERM_TYP   VARCHAR2(16)  DEFAULT 'GRID' NOT NULL,  -- 'GRID' | 'MAP'
    FIELD_NM   VARCHAR2(64)  NOT NULL,   -- GRID: DB 컬럼명 / MAP: 용어 키(Map.Slot 등)
    CAPTION_EN VARCHAR2(128) NOT NULL,
    CAPTION_KO VARCHAR2(128),            -- NULL = 영문 겸용
    USE_YN     CHAR(1)       DEFAULT 'Y' NOT NULL,
    UPD_USER   VARCHAR2(32),
    UPD_TM     DATE          DEFAULT SYSDATE,
    CONSTRAINT PK_UI_TERM_DIC PRIMARY KEY (TERM_TYP, FIELD_NM)
);
```

시드 데이터는 `GridCaptionDictionary.cs`의 하드코딩 항목과 동일하게 시작한다
(회사 적용 시 이 파일을 시작점 프로젝트에 복사하면서 INSERT로 변환).

기본 노선은 시드 등록 한 줄로 끝난다. DB 덮어쓰기(선택)를 쓰는 경우에만
②③을 더한다 — **DB 조회는 앱/프레임워크가 한다**. 로더는 `DataTable`만
받으며 DLL에는 DB 코드가 없다(컨트롤 계약의 DataSource 관용과 동일):

```csharp
// Program.Main — 앱당 1회, 첫 폼 생성 전
GridCaptionDictionary.RegisterAll();                       // ① 하드코딩 사전 (기본 노선은 이 한 줄로 끝)

// ── 이하 ②③은 DB 덮어쓰기 노선(선택)일 때만 ──
DataTable terms = FrameworkDb.Select(                      // ② 회사 DB 계층으로 조회
        "SELECT FIELD_NM, CAPTION_EN, CAPTION_KO FROM UI_TERM_DIC " +
        "WHERE TERM_TYP = 'GRID' AND USE_YN = 'Y'");
GridCaptionDictionary.Apply(terms, GridCaptionLanguage.English);   // ③ DB가 시드를 덮어씀

// 한글 화면 앱은 언어만 바꾼다: RegisterAll 대신 Apply(Korean) + Apply(terms, Korean)

// 맵 계열 용어(TERM_TYP='MAP')도 같은 방식 (Modern.Lab.Captions.MapCaptionDictionary)
Modern.Lab.Captions.MapCaptionDictionary.Apply(mapTerms);

// 폼 — 캡션 생략 = 사전 표준 캡션(없으면 자동 변환), 명시 = 화면 문맥 재정의(항상 우선)
this.grid.ConfigureColumns(
    new ModernDataGridColumn("ITEM_ID"),                 // 사전: "Lot Id"
    new ModernDataGridColumn("EVENT_TM", "Step Time")); // 이 화면만 다른 표현
```

데모(`Modern.Lab.Samples`)는 기본 노선만 쓴다 — 시드 등록 + 자동 변환 폴백.
(서버 용어 덮어쓰기 데모 `LoadServerTermsInBackground`는 노선 확정과 함께
제거했다, 2026-08-06. `Apply(DataTable)` 로더 API와 계약 검사는 유지된다.)

- **사전에 없는 필드는 읽기 좋은 표기로 자동 변환된다** (2026-08-06,
  `GridCaptionCatalog.Humanize`) — 대문자·숫자·언더스코어로만 된 DB 컬럼
  표기(`EQP_ID`)를 INITCAP 규칙("Eqp Id")으로 바꾼다. camelCase 등 다른
  표기는 그대로 둔다. 약어 예외는 없다 — 기계 변환 티가 나는 캡션이 "아직
  용어 미정의" 신호를 겸하므로, 자주 쓰는 컬럼은 사전에 진짜 용어를 채운다.
  필드 이름은 대소문자 무시로 찾는다.
- 용어를 시스템 테이블(INITCAP 일괄 적재)로 채우지 말 것 — 순수 변환의 출력을
  데이터로 얼리면 검수된 용어와 기계 생성 표기가 섞여 사전의 보증이 사라진다.
  변환은 위 폴백이 하고, 사전에는 사람이 검수한 용어만 담는다.
- `GridCaptionDictionary`는 회사 공통 표준어를 제공한다. 화면/제품 전용 필드는
  `GridCaptionCatalog.Register` 또는 `RegisterRange`로 추가하거나 덮어쓴다.
- 사전 조회로 부족한 해석 로직(다국어 리소스 등)이 필요하면
  `ModernDataGridColumn.CaptionResolver`에 커스텀 제공자를 등록한다 —
  등록 시 사전 대신 그 제공자가 쓰인다.
- 캡션 없는 컬럼 정의 + `ExportXlsx` 조합이면 사전 한 곳 수정으로 화면과
  엑셀 캡션이 함께 바뀐다.

### 엑셀 내보내기

```csharp
// 화면 그리드 보이는 대로(컬럼 순서·캡션·Format) 전체 결과를 .xlsx로 저장.
// CheckBox/Button 컬럼은 자동 제외 — 내보내기용 컬럼 목록을 따로 만들지 않는다.
this.gridItems.ExportXlsx(path, "Logistics & Request", this.resultData);
```

그리드와 무관한 임의 표를 저장할 때는 저수준 헬퍼를 직접 쓴다:
`Modern.Lab.Export.SimpleXlsxWriter.Write(path, sheetName, headers, rows)` —
외부 라이브러리 없이 진짜 Open XML .xlsx를 쓴다 (인라인 문자열 셀, 헤더 굵게).

---

## ModernPagination

그리드 하단 페이지 바. 좌측 [크기 드롭다운(선택)] "총 N건" + 우측 ◀ 1 2 3 ▶
(현재 페이지 중심 최대 7개).

| 멤버 | 설명 |
|---|---|
| `TotalCount` | 전체 건수 — 조회 응답마다 갱신하면 페이지 수 자동 계산 |
| `PageSize` | 페이지당 건수 (기본 20). **고정 크기가 표준** — 화면 높이 연동(자동 PageSize)은 리사이즈 과도 구간의 공백/스크롤바 깜빡임 때문에 권장하지 않는다 (2026-08-01) |
| `PageSizeOptions` | 페이지 크기 선택 드롭다운 항목 — 쉼표 구분 (예: `"10,25,50,100"`). 빈 값(기본) = 숨김. 크기 변경 시 보던 첫 행 유지 환산 + `PageChanged` 1회 |
| `CurrentPage` | 현재 페이지 (1부터, 범위 자동 보정) |
| `TotalCountFormat` | 좌측 건수 표기 형식. 기본 `"{0:N0} total"` — 한국어 화면이면 `"총 {0:N0}건"` 등으로 |
| `PageChanged` | 페이지가 바뀔 때 1회 발생 (버튼·코드 할당·크기 드롭다운 공통) |

**그리드 상태바(`ModernDataGrid.ShowStatusBar`)와의 역할 구분**: 둘 다 그리드 하단에
건수를 표시하므로 **한 그리드에는 하나만** 쓴다. 페이징이 필요한 그리드 → 페이지 바
(건수 + 페이지 이동), 페이징 없는 단순 목록 → 상태바 (건수 + 문맥 텍스트).

**로컬 페이징은 `GridPageBinder`와 함께 (권장)** — "전체 조회 + 페이지 조각
바인딩" 화면의 정형 배선(재진입 가드·페이지 보정·조각 슬라이스·재바인딩)을
공통화한 헬퍼(같은 네임스페이스). 폼에 `PageChanged` 핸들러가 필요 없다:

```csharp
// 폼 생성자 (InitializeComponent 이후)
this.pageBinder = new GridPageBinder(this.grid, this.pager);
this.pageBinder.PageBound += this.OnPageBound;        // 조각 후처리 필요 시(선택)

// 조회 후 — 페이지 보정 + 1페이지 바인딩까지 처리
this.pageBinder.SetRows(result);                      // 전체 행
this.pageBinder.SetRows(result, viewRows, keepPage);  // 필터 부분집합/페이지 유지
this.pageBinder.EnsureRowVisible(index);              // 재조회 후 행 포커스 복원
```

서버 페이징(페이지 이동마다 재조회)은 바인더 없이 `PageChanged`에서
`CurrentPage`/`PageSize`로 해당 페이지를 재요청한다.

배치: 그리드 아래 `Dock = Bottom`, 높이 32~36. 통계는 전체 결과 기준 유지가
관례. 컬럼 헤더 정렬은 현재 페이지 조각 안에서만 적용된다(페이징 화면의 표준
트레이드오프). 샘플: Logistics & Request. 자세한 정책과 바인더
멤버는 `migration/ModernPagination.md`.

---

## ModernKpiCard / ModernSummaryList

하단 통계 영역용 표시 컨트롤.

### ModernKpiCard — 제목 + 강조 값 카드

| 멤버 | 설명 |
|---|---|
| `Title` | 값 위 제목 (예: "조회 건수"). **카드보다 길면 말줄임한다**(2026-09-08 — 값·보조 문구와 같은 규칙). 값을 한정하는 짧은 곁말을 제목에 붙여도 좁은 배치에서 레이아웃이 깨지지 않는다 — 값 칸을 둘로 쪼개는 대신 쓸 수 있다 |
| `Value` | 강조 값 텍스트 — 포맷은 폼에서 (`count.ToString("N0")` 등) |
| `Footnote` | 값 아래 한 줄 보조 문구 — 비우면 그 줄이 사라진다 |
| `Tone` | 카드 색조(`"#RRGGBB"`) — 표면 쪽으로 살짝 섞어 배경을 만든다(원색 비중 85%, 테두리는 원색 55%로 한 단계 진하게). `Palette` 색은 이미 파스텔이라 결과도 연하다. 자리 구분용이고 색에 의미는 없다. `Flat`이면 무시 |
| `Flat` | true면 카드 테두리 제거 — `ModernCardPanel` 위에 올릴 때 (중첩 카드 방지) |
| `TrendValues` | 스파크라인 — 값 오른쪽 초미니 추세선(끝점 도트). 숫자 시퀀스 할당, null/2개 미만 = 숨김. 카드 폭 200px 이상 권장 |

```csharp
this.cardCount.Title = "조회 건수";
this.cardCount.Value = this.gridEmployee.RowCount.ToString();
this.cardCount.TrendValues = new int[] { 12, 15, 11, 18, 16, 21, 17 };  // 최근 7일 추이

string[] tones = Modern.Lab.Theming.Palette.GetColors(4);   // 카드 여러 장을 자리로 구분할 때
this.cardLot.Tone = tones[2];
this.cardLot.Footnote = "FLW-QUICK · OPR-030";

// 값 칸에 둘을 넣기엔 좁을 때 — 주된 값만 값 칸에 두고 곁말은 제목에 붙인다(말줄임된다).
this.cardDurable.Title = "Durable · FOUP  from F-P1-007";
this.cardDurable.Value = "F-P1-003";
```

### ModernSummaryList — 구분·건수 칩 목록

| 멤버 | 설명 |
|---|---|
| `DataSource` | 구분 컬럼 + 건수 컬럼을 가진 행 목록 (서버 GROUP BY 결과) |
| `DisplayMember` / `ValueMember` | 칩 라벨 컬럼 / 건수 컬럼 |
| `ColorMember` | 칩 배경색 컬럼 (선택; hex/색 이름 문자열, 비우면 기본색). 글자색은 배경과 같은 색상 계열로 자동 산출 (연파랑 → 진파랑 글씨) |
| `Title` | 칩 왼쪽 제목 (비우면 숨김) |
| `Flat` | 카드 테두리 제거 |
| `ChipShape` | 칩 모양 — `Rounded`(둥근 사각, 기본) / `Pill`(알약). 값은 `Modern.Lab.Controls.Wpf.Common.ChipShape` |

```csharp
this.listDeptCount.Title = "부서별";
this.listDeptCount.DisplayMember = "DEPT_NAME";
this.listDeptCount.ValueMember = "CNT";
this.listDeptCount.ColorMember = "COLOR";         // 선택 — 행별 "#DBEAFE" 같은 값
this.listDeptCount.DataSource = deptCountTable;   // 조회할 때마다 재할당
// → [경영지원팀 4] [개발1팀 6] ... 칩으로 표시 (COLOR 컬럼이 있으면 부서마다 다른 색)
```

---

## ModernStepIndicator

가로 진행 단계 표시(스텝 인디케이터). 공정/처리 흐름을 한 줄로 보여 "지금 어디까지
왔는지"를 한눈에 파악하게 한다. 직접 대응하는 WinForms 컨트롤은 없다.

| 멤버 | 설명 |
|---|---|
| `DataSource` | 단계 행 목록 (DataTable/DataView/IList/IEnumerable) |
| `DisplayMember` | 단계 이름 컬럼/속성 |
| `StateMember` | 단계 상태 컬럼/속성 — 값은 문자열 `Completed` / `Current` / `Pending` / `Failed` (대소문자 무시) |
| `LabelFontSize` | 레이블 글자 크기 (2026-08-13 추가, 기본 0 = 테마 토큰 `Font.Size.Label`). 키우기/줄이기 모두 가능. 셀 폭 강등 사다리는 그대로라 크게 키우면 말줄임이 일찍 오고, 그 경우에도 툴팁이 전체 텍스트를 보여 준다. 노드 원 안의 숫자/글리프 크기는 바뀌지 않는다 |

상태별 모양: `Completed` 성공색(초록) 틴트 채움+번호, `Current` 진한 액센트
채움+흰 번호, `Pending` 회색 번호, `Failed` 빨강+X. 완료 연결선은 성공색,
현재 단계에 도달하는 연결선은 액센트색으로 표시한다. 색·글리프는 전부 디자인
토큰에서 온다. 3자리 이상 번호 노드는 알약형으로 늘어난다.

**폭 자동 강등**: 가용 폭이 부족하면 표시를 자동으로 강등해 어느 폭에서도
첫/현재/마지막 단계가 잘리지 않는다 (폼 코드/API 변경 없음, 전부 자동):

| 가용 폭 | 표시 |
|---|---|
| 넉넉함 | 스텝당 118px, 전체 레이블 |
| 부족 | 균등 축소(하한 48px) — 레이블 말줄임, 전체 텍스트는 툴팁. 하한이 64px였을 때는 5단계 기준 320px 경계에서 레이블 대부분이 한꺼번에 사라지는 절벽이 있었다 — 말줄임이 이 단의 문법이므로 짧아진 레이블이라도 유지하는 쪽이 덜 잃는다 |
| 더 부족 | 적응 접기 — 폭 배분 우선순위: 첫/현재±1/마지막 노드 → 현재 레이블 → 직전 "최근" 레이블(합쳐 최대 5개, 셀 92px) → 남는 폭은 현재 주변 노드(40px). 안 들어가는 구간만 `⋯` 노드로 접음(툴팁에 접힌 단계 목록) |
| 극단적으로 부족 | 압축 — 레이블 전부 생략, 노드 폭 26~40px (5스텝 기준 ~130px까지 성립) |

기준 단계는 `Current` → `Failed` → 마지막 `Completed` 순으로 정한다.
샘플 갤러리의 "Step Indicator" 화면에서 스텝 수별 비교와 폭 슬라이더 테스트를
확인할 수 있다.

```csharp
this.stepFlow.DisplayMember = "LABEL";
this.stepFlow.StateMember = "STATE";

DataTable steps = new DataTable();
steps.Columns.Add("LABEL", typeof(string));
steps.Columns.Add("STATE", typeof(string));
steps.Rows.Add("Created", "Completed");
steps.Rows.Add("Released", "Completed");
steps.Rows.Add("JobEnd", "Current");     // 마지막 = 현재 단계
this.stepFlow.DataSource = steps;         // → ●─●─◎ 진행 바
```

배치: 상세 카드/그리드 위에 `Dock = Top` 스트립(높이 약 56). 데이터의 실제 이벤트를
시간순으로 넣으면 마지막을 `Current`(또는 중단이면 `Failed`)로 표시하는 식으로 쓴다.

---

## ModernProgressBar

확정형 진행 바 — `ProgressBar`의 드롭인 대체. Fluent식 얇은 트랙(6px pill) +
액센트 채움. CAPA 사용률·수납율처럼 "몇 / 몇"이 있는 표시에 쓴다.
진행률을 모르는 대기 표시는 `ModernSpinner`/`ModernBusyOverlay`(Marquee 대체).

### 속성

| 멤버 | 타입 | 설명 |
|---|---|---|
| `Minimum` / `Maximum` / `Value` | int | ProgressBar 동일. `Maximum ≤ Minimum` = 빈 트랙 |
| `ShowText` | bool | 우측에 "NN%" 표기 (기본 꺼짐) |

```csharp
this.barOccupancy.Maximum = slotTotal;
this.barOccupancy.Value = slotUsed;
```

권장 크기: 폭 자유 × 14~16. 샘플: Carrier Editor 카드 하단 수납율 바.

---

## ModernStatusBadge

상태 표시 배지 (색 있는 `Label` 대체). 배경색만 주면 **글자색이 자동 유도**된다
(같은 색상 계열의 반대 명도 톤; 대비가 4.5:1에 못 미치는 중간톤 배경은
검정/흰색으로 자동 강등 — 2026-08-07).

| 멤버 | 설명 |
|---|---|
| `Text` | 배지 텍스트 (`Control.Text` override, localizable) |
| `Color` | 배경색 (hex/색 이름 문자열; 비우면 중립 회색). **직접 색 — 세 색 경로 중 최우선** |
| `Semantic` | 의미색 (`SemanticKind` — Success/Warning/Error/Info/Neutral, 기본 None=미사용). `SemanticColors` 표준색을 헥사 없이 디자이너/코드에서 지정. Color 다음 순위 |
| `ColorValue` | **값 유도 자동색** — 값 문자열을 넣으면 `Palette.FromValue` 규칙으로 색이 정해진다 (같은 값 = 어떤 화면에서든 같은 색, 겹치는 색 자동 회피). 그리드/트리 배지의 `BadgeAutoColor`와 같은 규칙이라, 그리드와 같은 값을 넣으면 KPI 집계 배지·정보 카드 배지가 그리드 배지와 자동으로 색이 맞는다. 마지막 순위 (우선순위: `Color` > `Semantic` > `ColorValue`) |
| `AccentValues` | **빨간 글자 강조 값 목록** (2026-08-09 추가, 세미콜론/쉼표 구분). `Text`가 목록에 있으면 배지를 진한 오류 채움으로 바꾸고 테두리를 두른다 — `Color`/`Semantic`/`ColorValue`보다 앞선다. 그리드 배지의 `BadgeAccentValues`와 같은 규칙 |
| `Spinning` | 배지 테두리를 도는 하이라이트(코멧 빛띠) 표시 (기본 false) — 작업중/전송중 표시. 그리드 배지의 `BadgeSpinBorderMember`/`BadgeSpinValues`와 같은 시각 언어. 보일 때만 타이머가 돌고 숨김/Dispose 시 자동 정지. **2026-08-11 — 빛띠가 끊겨 돌던 것 수정**: 둘레를 고정 간격으로 나눠 칠하므로 긴 직선 변에서도 알파가 이어지고, 위상을 틱 수가 아니라 경과 시간으로 재므로 UI 스레드가 바빠 타이머가 틱을 걸러도 속도가 변하지 않는다(프레임만 줄어든다). 숨겼다 다시 보이면 처음이 아니라 있던 자리에서 이어진다 |
| `SpinValues` | **회전을 켤 값 목록** (2026-08-22 추가, 세미콜론/쉼표 구분). `Text`가 목록에 있으면(공백/대소문자 무시 — `FromValue`와 같은 정규화) `Spinning`이 자동으로 켜지고 벗어나면 꺼진다 — 그리드 컬럼 `BadgeSpinValues`와 같은 문법·의미의 선언형 경로. 비어 있지 않으면 Text가 바뀔 때마다 재판정하므로 `Spinning` 직접 대입은 다음 Text 변경 때 덮어써진다; 비우면(기본) `Spinning`은 수동 스위치 |
| `Shape` | 모양 — `Rounded`(둥근 사각, 기본) / `Pill`(알약). 라이브러리 전반의 배지/칩 기본 모양은 둥근 사각으로 통일 |
| `FillWidth` | 알약을 항상 컨트롤 폭으로 그릴지 (기본 `false` = 텍스트 폭에 맞춤). KPI 스트립처럼 나란한 배지들의 폭을 텍스트 길이와 무관하게 통일할 때 `true` |

텍스트가 배지 폭을 넘으면 말줄임(`…`)으로 그려지고 **전체 텍스트가 자동으로
마우스 오버 툴팁에 노출**된다 (별도 설정 없음) — 다이얼로그의 결과/오류
배지처럼 서버 실패 사유가 길게 오는 자리에서 전문을 읽을 수 있다.

```csharp
// 값 유도 자동색 — 그리드의 "Sent" 자동색 배지와 항상 같은 색 (KPI 집계 배지).
this.badgeTransit.ColorValue = "Sent";
this.badgeTransit.Text = "Sent 12";

// 의미색 — 헥사 없이 표준 의미색 (디자이너에서도 지정 가능).
this.badgeUnmatched.Semantic = Modern.Lab.Theming.SemanticKind.Error;

// 직접 색 — 데이터가 주는 색을 그대로 쓸 때 (최우선).
this.badgeStatus.Color = "#DCFCE7";   // 연초록 배경 + 진초록 글씨(자동)

// 작업중 표시 — 테두리를 도는 하이라이트 (수동 스위치).
this.badgeSending.Spinning = true;

// 값 선언형 — Text가 "Run"/"Sending"일 때만 자동으로 돈다.
this.badgeStatus.SpinValues = "Run;Sending";
this.badgeStatus.Text = status;
```

권장 크기: 텍스트에 맞는 폭 × 26 (배지 세로 패딩 4px 기준).

---

## ModernSlotMap

캐리어(운반체)의 수납 구조를 **실물 단면처럼** 그리는 슬롯 맵 (신규 개념 컨트롤).
구획(`SlotMapSection`)별 셀 격자 — `Columns = 1`이면 세로 사다리, 5면 5×5 격자.
구획의 **`Kind`(`SlotMapSectionKind`)가 실물 표현**을 정한다: `WaferEdge`(FOUP —
레일 사이 웨이퍼 바, 빈 슬롯은 레일 홈) · `PinStub`(STUB — 원판 위 사각 칩,
칩 안에 웨이퍼 ID 끝 4자리 표기, 빈 스텁은 핀 자국만) · `LamellaPost`(LCC —
포스트에 라멜라가 붙고 **삽입 위치
Top/Left/Right가 붙는 위치 자체**) · `Generic`(기본 상자 표현 — 기존 화면 호환).
상태(채움/빈/미리보기/선택)는 형태 차(도형 유무·점선·링)로 인코딩해 테마와
무관하게 읽힌다. 상세는 호버 툴팁.

| 멤버 | 설명 |
|---|---|
| `SetSections(SlotMapSection[])` | 구획/셀 모델을 통째로 다시 그린다 (재조회 반영 경로; 선택·미리보기 초기화) |
| `AllowSelection` | 채워진 셀과 미리보기 셀의 클릭 허용 (기본 true) — 보기 전용 맵은 false |
| `EnableDragOut` / `AcceptDrops` | 드래그앤드롭 — 원본 맵에서 끌기 허용 / 대상 맵에서 드롭 수용 (둘 다 기본 false) |
| `SelectedKeys` / `ClearSelection()` | 선택된 셀 키(`SlotMapCell.Key`) 배열 / 전체 해제 |
| `SetPreview(Dictionary<string,string>)` | 자리 키(`"Wafer\|7"` / `"Chip\|3"` / `"Lamella\|3\|A"`) → 들어올 웨이퍼 ID 미리보기 맵 — 빈 자리에 **같은 실물 도형을 점선 윤곽 + "→ ID"**로 그려 확정 전 계획을 형태로 구분한다. 계획된 자리가 부족하면 "need n more" (null = 해제). 화면이 커밋과 같은 배치 규칙으로 계획을 넘겨 미리보기와 실제 이동 결과가 일치한다 |
| `SetPreviewMarkers(Dictionary<string,string>)` | LCC 미리보기의 자리 키 → 삽입 위치(`Top`/`Left`/`Right`) 맵. `SetPreview` 뒤에 설정하면 미리보기 라멜라(점선)가 그 위치에 붙는다 — 지정이 없으면 Top 위치 (null = 해제) |
| `SetSelectedKeys(string[])` | 스테이징 강조(강한 액센트) 지정 — 외곽선은 액센트, 슬롯 번호 배지는 한 단계 진한 액센트로 채워진다(작은 번호의 대비 여유). 이벤트 없음 |
| `SetClickKeys(string[])` / `SetClickKey(string)` | 다중/단일 클릭 선택을 고대비 경고색 외곽선 **+ 같은 색 슬롯 번호 배지**로 표시. 미리보기 셀은 `✓`를 함께 표시하고 채움색은 바꾸지 않으며 이벤트 없음. 배지 글자색은 고정 토큰이 아니라 **배지 배경에서 유도**하므로 테마를 더해도 4.5:1 아래로 내려가지 않는다 |
| `CellClicked` | 채워진 셀 또는 미리보기 셀 클릭 시 해당 셀 키 전달. 선택 상태는 화면이 관리한다 |
| `SelectionChanged` | 선택 변경 시 |
| `WafersDropped` | 드롭 수신 시 — 끌려온 셀 키들 + 놓은 자리(앵커) 셀 키. 검증/이동은 폼이 서버 호출로 |
| `CellRightClick` | 맵 오른쪽 클릭 시 — 커서 아래 채움 셀 키(셀 밖이면 빈 문자열). 폼이 이동 컨텍스트 메뉴를 띄우는 데 쓴다 |

LCC(SubCells) 구획 헤더에는 **"Lamella Id" 스위치**가 붙는다 — 켬(기본)은 셀마다
핑거당 웨이퍼 ID 행("A · LM-…")을 붙이고, 끄면 포스트+라멜라만 남는다(ID는 호버
툴팁으로). 상세와 모델(`SlotMapSection`/`SlotMapCell`/`SlotMapSubCell`) 설명·사용
예는 `docs/migration/ModernSlotMap.md` 참고. 풀 맵 정규화 헬퍼는
`SUB_PROD_TYP`·`SLOT_NO`·`FINGER_ID`·`FINGER_INDEX`·`WF_ID`·`LOT_ID` 스키마를 사용한다.
참조 구현: 샘플 Carrier Editor.

---

## ModernHandlerLayout

메인 핸들러(포트 IN n / OUT n 안에서 내부 장비 F1~F4가 제어되는 특수 장비)의
물리 구조를 그리는 **구조도** (신규 개념 컨트롤). 물류 흐름 순서(위 → 아래)로
**인포트 칩 → 내부 장비 타일(자리 게이지 + Lot) → 아웃포트 칩**을 그린다.
타일은 **상태와 점유를 다른 신호로** 그린다 — 상태(`Down`/`Run`/`Idle`)는 테두리와
하단 문구가, 점유는 게이지 칸의 형태가 말한다(채움 = 지금 앉은 Lot, 테두리만 =
준비만 끝난 예약, 흐림 = 빈 자리). 상태는 색에만 의존하지 않는다: 빈 포트/유휴
타일은 흐린 테두리 + 빈 게이지, 가동 타일은 액센트 테두리 + 채운 게이지, Down
타일은 위험색 테두리 + 흐린 게이지, 완료(Done) 포트는 선택 틴트 채움.
데이터는 SlotMap과 같은 **수동 모델** 방식 — 폼이 서버 조회 결과를
`HandlerLayoutPort`/`HandlerLayoutUnit` 목록으로 변환해 통째로 준다.

| 멤버 | 설명 |
|---|---|
| `SetLayout(ports, units)` | 구조도 데이터를 통째로 교체하고 다시 그린다 (재조회 = 재구성; 포트 선택 표시 초기화; null은 빈 목록) |
| `ClearLayout()` | 구조도를 비운다 (안내 문구만 남는다) |
| `UnitClicked` | 내부 장비 타일 클릭 — `HandlerUnitEventArgs.WaferId`. 선택 상태는 만들지 않는다 (모니터링 용도) |
| `PortClicked` / `PortRightClick` | 포트 칩 클릭/우클릭 — 칩에 선택 표시(포커스 테두리)가 걸리고 `HandlerPortEventArgs`(`Label` 등)로 알린다. 폼이 포트 상세 행 선택과 동기화해 포트 처리 대상으로 삼고, 우클릭은 포트 컨텍스트 메뉴를 띄운다 |
| `SetSelectedPort(label)` | 포트 칩 선택 표시 지정 (null = 해제) — 그리드 행 선택과 칩 표시 동기화용 |

`HandlerLayoutUnit`은 `Id` · `State`("Down"/"Run"/"Idle") · `Capacity` ·
`LotIds`(지금 앉은 Lot) · `ReservedLotIds`(준비만 끝난 Lot)를 갖는다. `State`를
비워 두면 Lot 유무로 Run/Idle을 파생한다.

설계 지침: **내부 장비(F)는 장비 그리드의 행이 아니다** — 장비 목록에는 부모
핸들러만 올리고 자식 현황은 요약 컬럼(Type/Capa "2/4")으로 말한다. 다만 선택
장비의 **자리 목록에는 자식 자리도 함께** 올린다(2026-08-03): 트레이가 안으로
들어가면 진행 중인 작업이 그 자리에 앉고, 수동 종료·취소의 지목 대상이 되므로
표시 전용 행이 아니다. 자식 행에는 투입·반출이 열리지 않는다. 포트 처리는 경로를 하나로 유지한다
— 포트 그리드를 숨기되 바인딩은 유지하고 칩 클릭을 그 그리드의 행 선택으로
변환하면, 포트 액션 활성/실행이 칩과 그리드 어느 쪽에서도 동일하다. 상세와
모델 설명·사용 예는 `docs/migration/ModernHandlerLayout.md` 참고.

> **참조 구현** — 샘플 `SubUnitDialogForm`(Equipment / Lots의 자식 장비 상세 창)이 이 컨트롤을
> 썼으나 그 화면과 함께 2026-08-29 삭제됐다. 사용 예는 `docs/migration/ModernHandlerLayout.md`의
> 코드 예시를 따른다.
>
> 메인 화면 포트 카드 안에서 포트 그리드와 바꿔 끼우지 <b>않는다</b>: ⓐ 조작
> 목록에 표시 전용 행이 섞이면 목록이 상태판이 되고, ⓑ 권장 높이(300)가 포트
> 카드 뷰포트보다 커서 카드 안에서는 스크롤이나 높이 불균형이 생긴다. 별도
> 창에는 높이 제약이 없다. 아래 "포트 그리드를 숨기되 바인딩은 유지" 지침은
> 카드 안에서 전환하던 시절의 것이다 — 별도 창에서는 구조도가 읽기 전용이라
> 포트 처리 경로와 얽히지 않는다.
>
> 모델 변환은 폼이 아니라 `EquipmentLotPresenter.BuildHandlerPorts/BuildHandlerUnits`가
> 한다 — 칩 상태 표기가 포트 그리드와 **같은 규칙**(`DisplayStat`)을 쓰게 하기
> 위해서다. 겸용(InOut) 자리는 투입 쪽에 한 번만 그린다.

---

## ModernSlotMapMini

캐리어의 수납 현황을 **다른 폼 구석에 올리는 표시 전용 미니 맵** (신규 개념
컨트롤). 큰 슬롯 맵(`ModernSlotMap`)이 세부 조작(선택/이동/드래그)을
담당한다면, 미니맵은 "지금 얼마나 차 있나"만 실물 형태로 한눈에 보여 준다.
연결은 DataTable 하나 — 수납 현황 조회 결과를 `DataSource`에 주면 그려진다
(채워진 자리 행만 와도, 풀 응답이 와도 동일). **수납 구조는 데이터(SUB_PROD_TYP)가
정한다**: `Chip`/`Lamella` 행은 TRAY 구획(원판 줄 + 핑거 격자), 그 외(`Wafer` 등)는
FOUP 슬롯 사다리(웨이퍼 바, 1~N 번호 기재). 구획(SLOT/STUB/LCC)마다 좌측
제목 + 우측 채움 집계가 붙은 **그룹박스 카드**로 싸이고(제목 글자는 폼
그룹박스와 같은 9pt), 구획 카드가 컨트롤 높이를 나눠 채워 **하단 여백 없이
딱 맞고 컨트롤을 늘리면 셀이 커진다**. 모든 자리에 호버 툴팁(자리 · 웨이퍼 ·
Lot).

| 멤버 | 설명 |
|---|---|
| `DataSource` | `DataTable`/`DataView`/`IList`/`IEnumerable` — 자리당 1행 (`SUB_PROD_TYP`/`SLOT_NO`/`FINGER_ID`/`FINGER_INDEX`/`WF_ID`/[`LOT_ID`]/[`ITEM_COLOR`]). 기존 `KIND`/`POS`/`FINGER`/`INDEX_POS`/`INS_POS` 및 `STUB`/`LCC` 표는 자동 폴백 |
| `SlotCapacity` / `StubCapacity` / `LccCapacity` | 자리 수 — **서버가 관리하는 슬롯 CAPA 값**(예: FOUP 25 / STUB 6 / LCC 25)을 그대로 지정 (수치 하드코딩 금지). 0이면 데이터에서 유도 |
| `KindMember` 외 멤버 이름 5종 | 기본은 공통 수납 컬럼(`SUB_PROD_TYP`/`SLOT_NO`/`FINGER_ID`/`WF_ID`/`LOT_ID`). 회사 컬럼명이 다를 때 재정의 (`KindMember`/`PositionMember`/`FingerMember`/`WaferMember`/`LotMember`) |
| `ColorMember` | 채움 색 컬럼 (기본 `"ITEM_COLOR"`, 선택) — 색은 폼이 정해 행에 담는다 (큰 맵과 같은 계약). 없으면 테마 기본 채움색 |
| `InsertMember` | Lamella 삽입 위치 컬럼 (기본 `"FINGER_INDEX"`, 선택) — 기존 표는 `INDEX_POS`, 그 이전 표는 `INS_POS`로 폴백. `Top`/`Left`/`Right`면 채운 핑거의 해당 변을 액센트로 굵게(큰 맵과 같은 방향 인코딩, 미니 축약) + 툴팁 표기 |

이벤트 없음(표시 전용). 구획 제목/안내/툴팁 용어는
`Modern.Lab.Captions.MapCaptionDictionary` **용어사전(영문)**을 따른다 —
회사 표준 용어가 기본값과 다르면 앱 시작 시 `Register(key, caption)`으로
재정의한다. 상세와 사용 예는 `docs/migration/ModernSlotMapMini.md` 참고.
참조 구현: 샘플 Slot Map (Mini) — FOUP/TRAY 타입 전환 + 창 리사이즈 스케일.

---

## ModernRequestInfo — 제거됨 (2026-08-12)

의뢰서 정보 카드 컨트롤은 **공통 라이브러리에서 제거**되었다. 의뢰서의 필드
구성은 회사 도메인이라 공통 DLL이 아니라 화면 소유물이어야 필드 추가/삭제가
쉽기 때문이다. 같은 기능은 샘플의 **의뢰서 정보 다이얼로그**
(`Modern.Lab.Samples/Dialogs/Requests/RequestInfoDialogForm.cs` + `.Designer.cs` +
`Services/RequestInfoTables.cs`)로 옮겨졌다 — 회사 프로젝트에
이 파일들을 복사해 넣어 쓴다. **현재 카드는 위 마스터 필드리스트(`ModernFieldList`) · 아래 시편 표
두 단**이고, 의뢰서와 시편을 조인해 받은 결과 한 벌을 경계 컬럼에서 갈라 그린다 —
컬럼을 늘리고 줄이는 일은 조회에서 하고 다이얼로그 소스에는 컬럼 이름이 없다.
여는 쪽은 `SetRequest(joined, 경계, 본문)` 후 `ShowDialog(this)` 한 쌍이다
(Logistics · LotHistory · Durable · Lot Management의 Req Serial No 링크 — 베이스 공용
`ShowRequestInfo(reqSerialNo)` 참고). Equipment/Lots는 이미 조회한 마스터·디테일 스냅숏을 전달한다.
자세한 경위와 교체 방법은 `docs/migration/ModernRequestInfo.md`.

---

## ModernBarcode

Code 128 바코드 표시 — LOT ID/캐리어 ID를 스캐너로 읽히게 띄우는 표시 전용
컨트롤 (GDI+ 순수 WinForms, 여러 개 놓아도 섬 비용 없음). 짝수 길이 숫자는
Code C 압축, 그 외 Code B(영숫자), 체크섬 자동. 막대는 **항상 흰 바탕 + 검정**
(스캐너 대비 조건 — 다크 테마에서도 흰 카드로 남는 것이 의도).
인코딩 불가 문자(한글 등 비ASCII)가 포함되면 **바코드를 그리지 않고 오류
안내를 표시**한다(fail-closed) — `IsValueEncodable`로 미리 검사 가능.

```csharp
this.barcodeLot.Text = lotId;        // "IT10002PP" — 비우면 그리지 않음
this.barcodeLot.ShowText = true;     // 막대 아래 값 텍스트 (기본 true)
```

권장 크기 280×76 (9자 기준 최소 240px — 모듈 폭 2px 이상이어야 스캔 안정).
자세한 크기 산정과 주의는 `migration/ModernBarcode.md`.

---

## ModernBusyOverlay

조회/처리 중 대상 영역을 덮는 로딩 패널 (스피너 + 메시지). 기본 숨김.

| 멤버 | 설명 |
|---|---|
| `Busy` | `true` = (`ShowDelayMs` 후) 표시 + 맨 앞으로, `false` = 숨김 (`MinShowMs` 유지 후) |
| `ShowDelayMs` | 표시 지연(ms, 기본 300) — 이 시간 안에 끝나는 짧은 작업은 오버레이가 아예 안 보여 깜빡임이 없다. 0 = 즉시 |
| `MinShowMs` | 최소 유지 시간(ms, 기본 500) — 표시 직후 곧바로 사라지는 잔상 방지. 0 = 즉시 숨김 |
| `Message` | 안내 문구 (기본 `"Processing..."`) — 조회·처리 양쪽에 쓰이는 컨트롤이라 `"Loading..."`(조회 전용)이 아닌 범용 동사를 기본값으로 둔다. 실제 화면은 구체적인 문구를 넘기는 것을 권장 |

배치: 덮을 영역과 같은 `Dock`으로, **그리드보다 먼저 `Controls.Add`** (z-순서 위).

```csharp
this.busyOverlay.Busy = true;                  // 조회 시작
// ... 백그라운드 조회 → Invoke로 결과 반영 ...
this.busyOverlay.Busy = false;                 // 완료
```

반투명은 ElementHost 제약으로 불가 — 대신 호스트가 **카드 모양(둥근 Region)으로
클리핑**되어 카드 바깥 사각 영역 자체가 없다. 어떤 테마/배경 위에서도 둥근
카드(스피너 + 메시지)만 떠 보인다. 스피너는 **옅은 트랙 링 + 코멧 테일 아크**(꼬리는
투명하게 사라지고 머리는 액센트로 짙어지는 140° 아크)가 등속이 아니라 "빠르게 돌고
잠깐 미끄러지는" 리듬으로 회전한다.

> **안 뜨는 것처럼 보인다면** 대개 정상이다 — 조회가 `ShowDelayMs`(300ms)보다 빨리
> 끝나면 오버레이는 아예 표시되지 않는다. 동작을 눈으로 확인하려면 샘플 갤러리의
> **Busy Overlay** 화면에서 작업 시간과 두 파라미터를 직접 조절한다. 자세한 계측값은
> [migration/ModernBusyOverlay.md](migration/ModernBusyOverlay.md#동작-확인--눌러도-안-뜨는데요) 참고.

---

## ModernToast

자동 소멸 알림 (완료/안내용 `MessageBox` 대체). 부모 우하단에 표시 후 자동 숨김.

```csharp
this.toastMain.Show("저장했습니다.", Modern.Lab.Controls.Wpf.Display.ToastKind.Success);
this.toastMain.Show("먼저 선택하세요.", Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
```

종류: `Info`/`Success`/`Warning`/`Error` (색 아이콘). **수명은 종류별**이다
(2026-08-23 현장 보고 반영):

| 종류 | 수명 | 동작 버튼 |
|---|---|---|
| `Info` / `Success` | `DurationMs`(기본 2500) 후 자동 소멸 | — |
| `Warning` | `DurationMs`와 6초 중 큰 쪽 | 복사 |
| `Error` | **자동 소멸 없음(스티키)** — 닫기 ✕/카드 클릭으로 닫는다 | 복사 + 닫기 ✕ |

긴 문구는 폭 상한에서 **여러 줄로 접혀 전문이 보이고**, 마우스를 올리면 읽는 동안
타이머가 멈추며, 카드 클릭은 즉시 닫는다. 복사 버튼은 문구 전문을 클립보드에 담고
성공하면 아이콘이 잠깐 체크로 바뀐다.
토스트는 **흘려도 되는** 통지 전용이다 — 반드시 읽어야 하는 실패 사유와 예/아니오
확인은 아래 `ModernMessageDialog`를 쓴다(2026-08-04부터 `MessageBox`를 쓰지 않는다).

---

## ModernMessageDialog

**모던 메시지/확인 다이얼로그** (`MessageBox` 대체). 종류별 아이콘 원 + 제목 + 본문
구성이고, 본문은 자동 줄바꿈·스크롤되며 **드래그해 복사**할 수 있다. 창은 WinForms
폼이고 본문만 WPF(`ModernMessageDialogControl`)라, Enter/Esc는 폼의
`AcceptButton`/`CancelButton`이 처리한다.

**쓰는 자리**: 서버 실패 사유처럼 **길고 반드시 읽어야 하는** 문구, 되돌릴 수 없는
처리 전 확인. (배지는 짧은 상태 한 줄, 토스트는 흘려도 되는 통지.)

본문 글자는 14px(`Font.Size.BodyLg`), 제목은 16px이다. WPF 글리프는 같은 크기의 GDI 글자보다 옅게
그려져 12px 본문이 흐려 보였다(2026-09-03 실측 — 진한 획 픽셀이 GDI의 3분의 1). 글자 무게를 직접 재려면
`Modern.Lab.Samples.exe --diag-textcapture` → `%TEMP%\textcapture-diag.txt`(+ `.png`).

```csharp
// 폼 베이스를 상속한 화면 — 네임스페이스를 몰라도 된다
this.ShowErrorMessage("Receive failed", result.Message);      // 긴 사유 그대로
if (!this.Confirm("선택한 3건을 삭제할까요?", "Delete")) { return; }

// 직접 부르는 경우
Modern.Lab.WinForms.Controls.Dialogs.ModernMessageDialog.ShowWarning(
        this, "Nothing selected", "Select at least one row first.");
```

### 정적 진입점

| 멤버 | 설명 |
|---|---|
| `ShowInformation(owner, [title,] message)` | 정보 알림 (확인 하나) |
| `ShowSuccess(owner, title, message)` | 성공 알림 |
| `ShowWarning(owner, title, message)` | 경고 알림 |
| `ShowError(owner, title, message)` | 오류 알림 — 긴 사유를 그대로 넘긴다 |
| `ShowError(owner, title, message, details)` | 오류 알림 + **접어 둔 기술 상세** (2026-09-16 추가) — 본문은 현업이 읽고 행동할 수 있는 안내, `details` 는 개발자가 받을 원문 |
| `Confirm(owner, title, message)` | 예/아니오 → `bool` (Yes가 기본, Esc = No) |
| `Show(owner, kind, title, message, buttons)` | 종류·버튼 직접 지정 (`OK`/`OKCancel`/`YesNo`) |
| `Show(owner, kind, title, message, buttons, details)` | 위와 같고 기술 상세까지 |
| `Emphasis(text)` | 본문 안 낱말을 **액센트색 SemiBold**로 강조하는 표기(`**text**`)를 만든다 (2026-08-29 추가) — `Confirm(this, "Confirm", "Change " + id + " to " + Emphasis(mode) + "?")`. 복사·높이 계산은 표기를 걷어낸 순수 본문(`ModernMessageDialogControl.StripEmphasis`) |

`ModernMessageKind`: `Information` / `Success` / `Warning` / `Error` / `Question`
(아이콘·색 결정 — **제목만** 그 색으로 물들고 본문은 기본 글자색이다).
창 폭과 최소 본문 높이는 긴 오류 내용을 읽기 충분한 크기로 확보한다. 본문은 테두리가 있는
복사 박스 안에서 자동 줄바꿈되며, 약 네 줄을 넘으면 세로 스크롤이 나타난다. 복사 아이콘은
스크롤바 왼쪽의 박스 안에 고정된다.

**창 크롬**: OS 제목줄을 쓰지 않는다 — 얇은 헤더(34px)에 닫기 ✕를 두고 헤더를
드래그해 옮기며, 1px 테두리 + Windows 11 둥근 모서리다. 닫기 ✕는 Esc와
같은 결과를 낸다. 제목을 비우면 종류별 기본 제목이 창 이름(Alt+Tab용)에 들어간다.

**복사** (2026-08-23 현장 보고 반영): 본문 박스 우측 위 복사 버튼 또는 **Ctrl+C**가 제목+본문을
평문으로 클립보드에 담는다 — 네이티브 `MessageBox`의 Ctrl+C 전체 복사와 호환.
본문을 드래그해 선택한 상태의 Ctrl+C는 선택 부분만 복사한다(선택이 우선).
복사에 성공하면 버튼 아이콘이 잠깐 체크로 바뀐다.


**기술 상세** (2026-09-16): `details` 를 넘기면 버튼 줄 **왼쪽 끝**에 `Details` 가 생기고,
누르면 창이 **버튼 줄 아래로** 자라면서 읽기 전용 상세 박스와 `Copy` 가 나온다. 다시 누르면
접히고 창도 원래 크기로 돌아간다. `Copy` 는 제목·본문·상세를 **한 번에** 담는다 — 상세만
보내면 어느 화면 무슨 상황이었는지 알 수 없기 때문이다. `details` 가 비어 있으면 버튼 자체가
생기지 않는다. 현업에게 **내부 컴럼명·계약 용어를 보여 주지 않으면서** 개발자는 원문을
받기 위한 것이다.
---

## ModernPopover

보조 정보를 **화면에 상주시키지 않고** 클릭한 자리에서 띄우는 팝오버
(`Component` — 레이아웃을 한 픽셀도 쓰지 않는다). 의뢰서 정보 카드처럼 "몇 건만
확인하는" 정보를 우측에 늘 붙여 두는 대신, 그리드 행의 버튼에서 같은 카드를 띄운다.
내부는 `ToolStripDropDown` + `ToolStripControlHost`이므로 **바깥 클릭·Esc 닫힘이
기본**이고, 별도 `Form`과 달리 Alt+Tab·작업표시줄·소유 창 추적 문제가 없다.

```csharp
this.popover.Content = this.requestInfo;                  // 카드 소유권은 폼에 남는다
this.popover.PopoverSize = new Size(340, 500);
// 행 버튼 클릭에서:
this.requestInfo.SetRequest(LookupByLot(lotId));
this.popover.ShowAt(this.gridRequests, this.gridRequests.PointToClient(Cursor.Position));
```

| 멤버 | 설명 |
|---|---|
| `Content` | 띄울 컨트롤 — 팝오버는 자기 셸에 담아 보여 줄 뿐이고 **소유권은 폼**(값 세팅·재사용 그대로) |
| `PopoverSize` | 크기 (기본 320×460) |
| `Show(anchor)` / `ShowAt(anchor, position)` | 앵커 오른쪽 / 앵커 좌표계의 지정 위치(그리드 클릭 지점 등)에 띄운다. 화면 밖이면 안쪽으로 당겨진다 |
| `Close()` / `IsOpen` | 닫기 / 열림 여부 |
| `Opened` / `Closed` | 열림·닫힘 이벤트 (바깥 클릭·Esc 포함) |

**Dispose 순서**: 폼의 `Dispose(bool)`에서 **팝오버를 먼저** 정리한다 —
`ToolStripControlHost`가 자기 자식을 Dispose하므로 팝오버가 내용을 셸에서 빼낸 뒤에
카드를 정리해야 한다. 모달이 아니며 핀(고정) 기능은 없다 — 계속 띄워 둘 정보라면
팝오버가 아니라 화면에 붙일 대상이다.

---

## ModernToolTip

테마 일치 툴팁 — `ToolTip`의 드롭인 대체 (상속이라 API가 전부 그대로).
기본 노란 시스템 툴팁 대신 Surface 배경 + Border 1px 테두리 + TextPrimary
글자로 그리며, 색은 표시 시점에 토큰 사전을 읽어 전 테마 자동 대응.
여러 줄 텍스트(`\n`)도 지원한다.

```csharp
this.tipMain = new Modern.Lab.WinForms.Controls.Display.ModernToolTip(this.components);
this.tipMain.SetToolTip(this.btnDelete, "Delete the selected lots");
```

`ToolTipIcon`/`ToolTipTitle`/`IsBalloon`은 그리지 않는다(무시). `OwnerDraw`/`Popup`/
`Draw`는 내부에서 쓰므로 다시 설정하지 말 것. 자세한 내용은 `migration/ModernToolTip.md`.

---

## ModernMenuRenderer

`ContextMenuStrip`/`ToolStripDropDown`을 **테마 표면으로 그리는 렌더러**(2026-08-29 추가,
`Modern.Lab.WinForms.Rendering`). 메뉴 항목·이벤트 코드는 그대로 두고 렌더러만 꽂는다.

```csharp
this.menuEqp.Renderer = new Modern.Lab.WinForms.Rendering.ModernMenuRenderer();
```

- 체크 표시: 기본 렌더러의 **파란 상자 + 체크** 대신 **상자 없는 액센트색 체크
  글리프**(Segoe MDL2 Assets, 본문 크기).
- hover: 시스템 파란 그라데이션 대신 **액센트 12% 틴트** 둥근 사각(흰 표면에서 `#E0EFFA`), 누르는 순간은
  `SelectedBackground`(진한 틴트). 비활성 항목은 반응 없음.
- 이미지 여백은 표면색 단색, 테두리 `Border` 1px, 구분선 `BorderSubtle`.
- 글자색 `TextPrimary` / 비활성 `DisabledText`. 항목이 `ForeColor`를 직접 지정하면 존중.
- 메뉴 안의 **`ToolStripLabel`은 구획 제목**으로 그린다 — 그리드 헤더와 같은 옅은 띠 위에
  한 단계 작은 SemiBold 본문색(회색 글자만으로는 비활성 항목과 구분되지 않는다). 샘플 Equipment Port의
  "Mode", Lot의 "Create / Hold/NotOnHold / Execute".
- 그리드/트리가 메뉴 끝에 붙이는 공통 항목(복사·찾기)도 같은 렌더러로 그려진다.

자세한 내용은 `migration/ModernMenuRenderer.md`.

---

## ModernCardPanel

영역 그룹핑용 카드 컨테이너(흰 표면·옅은 테두리·radius 8). **순수 WinForms Panel**이라
어떤 WinForms 자식이든 담을 수 있다 — 조회조건 영역, 하단 영역을 감싸는 용도.

```csharp
ModernCardPanel searchCard = new ModernCardPanel();
searchCard.Dock = DockStyle.Fill;            // 또는 Location/Size/Anchor
searchCard.Padding = new Padding(12, 9, 12, 9);
searchCard.Controls.Add(this.lblName);       // 자식은 일반 Panel처럼 추가
```

팁: KpiCard/SummaryList를 카드 판넬 위에 올릴 때는 `Flat = true`로 개별 테두리를 끈다.

`AutoScroll = true`로 스크롤 영역을 만들어도 된다 — 스크롤·휠에서 전면을 다시
그리므로 카드 테두리·그림자 선이 내용과 함께 밀려 올라가지 않는다(윈도우가 기존
픽셀을 통째로 미는 방식이라 그냥 두면 화면 중간에 가로줄이 남는다).
`ModernGroupBox`도 상속으로 같은 처리를 받는다.

---

## ModernGroupBox

헤더 타이틀이 있는 카드 (`GroupBox` 대체). `ModernCardPanel` 상속의 **순수 WinForms
패널**이라 어떤 WinForms 자식이든 담을 수 있다.

```csharp
this.grpStats.Text = "조회 통계";              // 헤더 타이틀 (SemiBold + 구분선)
this.grpStats.Controls.Add(this.listDept);    // 자식은 일반 Panel처럼
// 기본 Padding(12, 40, 12, 12)이 헤더 아래 공간을 확보
this.grpDetail.TitleFontSize = 10f;           // 탭 헤더(10pt)와 위계를 맞출 때 (기본 9pt)
this.grpItems.TitleRightText = "Days as of 2026-07-17 09:30:00";  // 헤더 오른쪽 보조 텍스트 (기준 일시 등)
this.grpEqp.TitleGlyph = "\uE713";              // 타이틀 왼쪽 아이콘 (Segoe MDL2 Assets)
this.grpList.TitleBar = true;                     // 타이틀 왼쪽 액센트 세로 바 (폼 제목 ModernLabel.TitleBar와 같은 문법, 2026-08-29)
```

`TitleGlyph`(2026-08-09 추가)는 타이틀 **왼쪽**에 아이콘 글리프를 붙인다. 비어 있으면
자리를 차지하지 않으므로 이미 만든 화면의 카드 머리는 그대로다. 크기는
`TitleFontSize`를 따라가고 색은 타이틀과 같다 — 카드 머리를 두 색으로 쪼개지 않는다.
제목과의 간격은 4px이고, 세로는 제목과 같은 기준선에 맞춘다(제목이 순수 중앙보다
1px 낮게 그려지므로 글리프도 같은 만큼 내린다 — 안 맞추면 글리프가 떠 보인다).

헤더가 필요 없으면 `ModernCardPanel`, 헤더가 필요하면 `ModernGroupBox`.

---

## ModernSplitContainer

좌/우(상/하) 영역 크기를 드래그로 조절하는 스플리터 — `SplitContainer`의 대체
(순수 WinForms 컨테이너, GDI+). API는 `SplitContainer` 그대로이며 시각만 다르다:
거터는 부모 배경색(색 띠가 아닌 "간격"), 중앙에 그립 필 — 평상시 `BorderSubtle`,
오버/드래그 중 `Accent`. 드래그 후 점선 포커스 사각형이 남지 않는다.

드래그 방식 `DeferredDrag`(기본 true): 드래그 중 가는 액센트 가이드 라인만
움직이고 놓을 때 한 번 적용 — ElementHost가 많은 화면(리사이즈 스텝당 수백 ms)
에서도 드래그가 매끄럽다. 가벼운 화면은 `false`로 실시간 리플로우 가능.

비율 유지 `KeepRatio`(기본 false, 2026-09-03): 켜면 컨테이너 크기가 바뀌어도 **디자이너에서 정한 분할
비율**을 지킨다 — 창을 키우거나 다른 해상도·배율의 PC에서 열어도 왼쪽/오른쪽 비중이 설계와 같다.
사용자가 분할선을 끌면 그 비율이 새 기준이 된다. 기본 `SplitContainer`의 비례 리사이즈는 최소 크기에
걸리거나 처음부터 설계와 다른 크기로 열릴 때 비율을 잃는다(장비 표가 넓게 설계됐는데 창을 조정하면
어긋났던 현장 보고). 껍데기 4종의 좌/우 분할은 켜 두었다.

분할선 폭은 8(`Space.Sm`)을 권장한다 — 12에 패널 안쪽 여백 8까지 두면 표 사이가 너무 벌어진다(2026-09-03).

```csharp
this.splitMain.Orientation = Orientation.Vertical;   // 좌/우 분할
this.splitMain.Panel1.Controls.Add(this.leftZone);   // 트리/목록 등
this.splitMain.Panel2.Controls.Add(this.rightZone);  // 상세 영역
this.splitMain.Panel1MinSize = 240;
this.splitMain.Panel2MinSize = 480;
this.splitMain.SplitterDistance = 340;
```

주의: 코드로 직접 배치할 때는 디자이너처럼 **`ISupportInitialize.BeginInit()/EndInit()`로
감싸야 한다** — 초기 크기보다 큰 `MinSize`/`SplitterDistance`를 그냥 설정하면 예외.
자세한 내용과 `.Designer.cs` 예시는 `migration/ModernSplitContainer.md`.

---

## ModernTabControl

언더라인(피벗) 스타일 탭 컨테이너 — `TabControl`의 대체 (순수 WinForms, GDI+).
선택 탭은 액센트색 SemiBold + 밑줄, 색은 팔레트를 읽어 전 테마 자동 대응.
페이지는 `ModernTabPage`(=`TabPage` 대응, `Text`가 탭 제목)로 구성하며, 폼
디자이너에서 "Add Tab / Remove Selected Tab" 동사와 헤더 클릭 전환을 지원한다.
런타임 코드에서는 `AddTab(제목, 컨트롤)`도 그대로 쓸 수 있다.

```csharp
// .Designer.cs (디자이너 직렬화 — 권장):
this.tabHistory.Controls.Add(this.pageLotHistory);   // ModernTabPage, Text="Lot History"
this.tabHistory.Controls.Add(this.pageWaferHistory);   // ModernTabPage, Text="Wafer History"
this.pageLotHistory.Controls.Add(this.gridHistory);  // 그리드는 페이지의 자식

// 코드 비하인드:
this.tabHistory.SetTabTitle(1, "Wafer History — " + waferId);  // 데이터만 갱신, 전환 없음
this.tabHistory.SelectedIndexChanged += this.OnHistoryTabChanged;
this.pagePending.BadgeCount = pendingTable.Rows.Count;         // 탭 카운트 배지 (0 = 숨김, 99+ 캡)
```

자세한 멤버/주의는 `migration/ModernTabControl.md`.

---

## ModernExpander

접이식 그룹박스 — WPF `Expander` 대응 (WinForms 표준 대응물 없음).
`ModernGroupBox` 상속의 **순수 WinForms 패널**이라 어떤 자식이든 담고,
헤더 클릭으로 본문이 접히고 펼쳐진다(높이 토글 + 셰브론 표시). 자주 안 보는
상세 조건 영역을 평소에 접어 둘 때 쓴다.

```csharp
this.expFilters.Text = "Advanced conditions";
this.expFilters.IsExpanded = false;                     // 접기 (기본 true)
this.expFilters.ExpandedChanged += this.OnFiltersToggled;
```

| 멤버 | 설명 |
|---|---|
| `IsExpanded` | 펼침 여부 (기본 true). false = 헤더 한 줄(36px)만 남김 |
| `ExpandedHeight` | 펼칠 때 복원할 높이 (0 = 접는 시점 높이 자동 저장) |
| `ExpandedChanged` | 접힘/펼침 변경 이벤트 |

접힘은 높이를 줄이는 것이라 **아래 컨트롤이 따라 올라오려면 흐름형 배치**
(TableLayoutPanel AutoSize 행 / Dock=Top 스택)가 필요하다. 고정 좌표 배치에서는
빈 자리가 남는다. 자세한 내용은 `migration/ModernExpander.md`.

---

### 컬럼·필드를 안 적는 경우 (2026-08-31)

`ModernDataGrid`는 `ConfigureColumns` 없이 `DataSource`만 주면, `ModernFieldList`는
`DefineFields` 없이 `SetRow`만 주면 **데이터가 무엇을 보여 줄지 정한다.** 규칙은
`Modern.Lab.Controls.Wpf.Data.AutoColumns` 한 곳에 있다 — 캡션은 용어사전,
이름이 `_COLOR`로 끝나면 제외, `_TM`으로 끝나면 시각(`yyyy-MM-dd HH:mm:ss` + 가운데).
선언하면 선언이 이긴다. 자세한 것은 각 컨트롤의 교체 가이드.

## ModernFieldList

단건 상세 필드 목록 — Lot 정보 카드처럼 한 건의 레코드를 **캡션(회색) 위 +
값(SemiBold) 아래** 스택형 쌍으로 보여준다. 괘선·셀 배경 없이 굵기와 색으로
위계를 만드는 모던 상세 뷰다. 괘선 표 룩을 유지해야 하면 `ModernDetailTable`.

### 멤버

| 멤버 | 설명 |
|---|---|
| `Columns` | 열 수 (기본 2) |
| `DefineFields(...)` | `ModernFieldDefinition(캡션, 컬럼명[, columnSpan])` 쌍으로 배치 정의 — `columnSpan`으로 긴 값(제품명 등)을 한 줄 전체로 넓힌다. **캡션을 생략한 `ModernFieldDefinition(컬럼명[, columnSpan])`은 그리드 컬럼과 같은 용어사전**(`GridCaptionCatalog` → 폴백 `"EQP_ID"` → `"Eqp Id"`)에서 캡션을 읽는다 (2026-08-29 추가) |
| `ModernFieldDefinition(...) { IsLink = true }` / `FieldLinkClick` | **링크 필드** (2026-08-29 추가) — 값이 액센트색·**항상 밑줄**·손 커서(호버는 색만 진해진다), 왼쪽 클릭 시 `FieldLinkClick(Member, Value)`. 그리드 Link 컬럼과 같은 역할(Req Serial No → 의뢰서 팝업). 빈 값은 링크가 아니다 |
| `ModernFieldDefinition(...) { IsBadge = true }` / `BadgeAccentValues` / `BadgeSpinValues` | **배지 필드** (2026-09-15 추가) — 값 자리에 `ModernStatusBadge`가 놓인다. 색은 값에서 유도되므로(`ColorValue`) 그리드 배지의 `BadgeAutoColor`와 같은 규칙이고 **같은 값이면 표와 카드의 색이 맞는다**. 강조(`BadgeAccentValues`)·회전(`BadgeSpinValues`)은 그리드 배지와 같은 문법이다. 값이 비면 "-", `IsLink`와 겹치면 배지가 이긴다 |
| 배지 필드의 수명 | `IsBadge` 는 `DefineFields` 시점에 읽힌다(`IsLink` 는 매번 읽는다). 조회마다 `FieldDefinitions.Apply` 를 불러도 정의가 같으면 배지를 다시 만들지 않아 회전 위상이 유지된다 |
| `SetRow(DataRow)` | 행에서 값을 읽어 채움 (없는 컬럼/빈 값 = "-") |
| `SetValue(member, value)` / `ClearValues()` | 개별 값 지정 / 전부 "-" |

```csharp
this.fieldLotInfo.Columns = 2;
this.fieldLotInfo.DefineFields(
    new ModernFieldDefinition("Product", "MODEL_ID", 2),   // 한 줄 전체
    new ModernFieldDefinition("Status", "STAT_TYP"));
this.fieldLotInfo.SetRow(row);
```

조회 결과를 그대로 실을 때는 빌더가 짧다 — 배지도 같은 어휘다.

```csharp
FieldDefinitions.Of(lots)
        .Badge("MES_PROC_STAT_CD")
        .BadgeAccent("MES_PROC_STAT_CD", "HOLD")
        .BadgeSpin("MES_PROC_STAT_CD", "PROC;SENDING")
        .Apply(this.fieldInfo);
```

권장 크기: 폭 자유 × (행 수 × 38). 샘플: Manual Receive의 Lot 정보 카드, Lot History의 Selection 상세(`fieldDetail`).

---

## ModernDetailTable

캡션/값 상세 표 — `TableLayoutPanel`의 드롭인 대체 (순수 WinForms).
디자이너 사용법(행/열, 셀 배치, 열 병합)은 표준과 같고 그리기만 다르다:
테마 팔레트 괘선 + 캡션 셀(`ModernLabel Kind=Label`) 헤더 톤 자동 칠하기,
열 병합 내부 세로선 생략. 폼마다 `CellPaint` 커스텀 페인트 코드를 들고 다닐
필요가 없다.

```csharp
// .Designer.cs: 타입만 바꾸면 끝 (CellPaint 연결·핸들러는 삭제)
this.tblDetail = new Modern.Lab.WinForms.Controls.Layout.ModernDetailTable();
// 캡션 = ModernLabel(Kind=Label), 값 = ModernLabel(Kind=Body)로 셀에 배치
```

**문서형 읽기 화면의 단일 캡션 레일(`캡션|값` 2열)용**이다 — 작업 다이얼로그
속 단건 요약이나 좁은 폭의 2단 레일 배치에는 `ModernFieldList`를 쓴다.
자세한 교체 예시와 쓰임 경계는 `migration/ModernDetailTable.md`.

---

## ModernDataGridColumn

그리드(`ConfigureColumns`)와 콤보 드롭다운(`ConfigureDropDownColumns`)이 공유하는
컬럼 정의. **나열하는 만큼 컬럼이 생긴다 — 3개, 4개, 그 이상도 가능.**

**폭 지정 지침**: `AutoFitColumns`를 켠 그리드는 폭을 **생략**한다 — 텍스트/배지/
버튼 컬럼 폭은 헤더+데이터 실측으로 재계산되어 숫자를 적어도 무시된다(죽은 값).
헤더가 폭을 결정하는 컬럼(값이 캡션보다 짧은 `Duration` 같은 컬럼)은 값 필터 깔때기
몫까지 예약된다. 정렬 글리프(▲/▼) 몫은 **정렬이 걸린 컬럼에만** 예약하므로, 헤더를
클릭해 정렬하면 그 컬럼이 글리프 폭만큼 넓어진다(평상시 폭을 좁게 유지하기 위한 선택).
폭이 실제로 쓰이는 곳(AutoFit 끈 그리드, AutoFit 그리드의 CheckBox 컬럼)에서는
숫자 대신 시맨틱 프리셋 **`GridWidths`** 를 쓴다: `Check`(44) · `Status`(84) ·
`Code`(96) · `Id`(130) · `Name`(150) · `DateTime`(150).

```csharp
new ModernDataGridColumn("CHK", "", GridWidths.Check) { Kind = GridColumnKind.CheckBox }
new ModernDataGridColumn("EVENT_TM", "Event Time", GridWidths.DateTime)   // AutoFit 아닌 그리드
```

**다중 줄 헤더**: 헤더 캡션에 `"\n"`을 넣으면 2줄 이상 헤더가 된다 — 헤더 높이는
그리드가 최대 줄 수에 맞춰 자동으로 늘리고, `VisibleRowCapacity`(페이지 크기 연동)와
AutoFit 폭 측정(최장 줄 기준)에도 반영된다. 줄바꿈 위치는 명시적 `\n`만 지원한다
(폭 기준 자동 래핑은 AutoFit과 순환 의존이 생겨 지원하지 않음).

```csharp
new ModernDataGridColumn("EVENT_TM", "Event\nTime")   // 2줄 헤더
```

| 생성자/속성 | 설명 |
|---|---|
| `new ModernDataGridColumn(컬럼명, 헤더)` | 폭 생략 — AutoFit 그리드의 기본 형태. AutoFit이 꺼져 있으면 남은 공간 채움(star). 헤더에 `"\n"` = 다중 줄 |
| `new ModernDataGridColumn(컬럼명, 헤더, 폭)` | 픽셀 고정 폭 — 숫자 대신 `GridWidths` 프리셋 권장 |
| `new ModernDataGridColumn(컬럼명, 헤더, 폭, 형식)` | **표시 형식까지 한 줄로** (2026-08-12 추가) — `Format` 속성 대입과 완전히 같다. 날짜/숫자 컬럼용: `new ModernDataGridColumn("SENT_DT", "Sent", 140, "yyyy-MM-dd HH:mm")` |
| `TextAlignment` | `Left`(기본) / `Center` / `Right` |
| `Format` | 표시 형식 — 숫자 `"N0"`/`"N2"`, 날짜 `"yyyy-MM-dd"` 등. 타입 컬럼(int/decimal/DateTime)은 그대로 적용되고, **문자열 컬럼이라도 값이 날짜/숫자로 해석되면 적용된다** — 서버가 `"2026-07-29 12:00:00.0"` 같은 java Timestamp 문자열로 내려보내도 폼은 `Format = "yyyy-MM-dd HH:mm"`만 선언하면 된다 (해석 실패 시 원본 그대로, 예외 없음; 화면·복사·찾기·엑셀 내보내기 모두 같은 규칙). 정렬은 형식과 무관하게 원본 값 기준 |
| `Kind` | 셀 표시 종류 — `Text`(기본) / `CheckBox` / `Badge` / `Button` / `Combo` / `Spinner` / `Link` (그리드 전용; 아래 표) |
| `ReadOnly` | Text 셀 전용 컬럼 잠금 (기본 false) — 그리드가 편집 모드(`ReadOnly = false`)일 때 이 컬럼만 편집을 막는다. 키 컬럼(ID 등)용. 그리드가 읽기 전용이면 이 값과 무관하게 전부 잠김 |
| `Frozen` | 가로 스크롤 시 왼쪽 고정 (기본 false) — `DataGridViewColumn.Frozen` 대응. 고정은 왼쪽부터 연속 (중간 컬럼만 켜면 그 왼쪽까지 함께 고정). 넓은 그리드의 키 컬럼용 |
| `MergeCells` | **같은 값 세로 셀 병합** (기본 false) — 현재 뷰 순서의 연속 동일 값 구간을 **하나의 스팬 셀**(단일 표면 + 세로 중앙 값, 내부 구분선 없음)로 그린다 — 엑셀/Spread 병합과 같은 시각. 정렬·필터·편집·스크롤 자동 추종. 클릭/선택/복사/엑셀은 행 단위 그대로(전 행 선택 시 스팬 셀도 선택색 — 부분 선택은 중립, 병합 그리드 표준). 클릭하면 스팬 전체 하단에 현재 셀 언더라인. 정렬된 키/그룹 컬럼용. 샘플: Control Gallery Data 탭 Equipment 컬럼 |
| `Aggregate` | **집계 푸터** 종류 (기본 None) — `GridAggregateKind.Sum`/`Average`/`Count`/`Min`/`Max`. 지정 컬럼 위치에 정렬된 집계 행이 하단에 생기고, 뷰(필터 반영)/편집/행 추가·삭제를 따라 자동 재계산. 서식은 컬럼 `Format`. 샘플: Grid Editing (Qty 합계·Purity 평균) |
| `HeaderGroup` | **2단 그룹 헤더** 캡션 (기본 없음) — 이웃 컬럼들이 같은 캡션을 쓰면 그 구간 위에 그룹 줄이 스팬된다. 폭 조절/자동 맞춤/가로 스크롤/고정 컬럼과 동기화. 표시 전용(정렬·필터·복사·엑셀은 하위 캡션 기준). 그룹이 고정 경계를 걸치지 않게 구성. 샘플: Grid Editing (Inspection/Reference) |
| `TextColor` | Text 셀 전용 컬럼 글자색 — `"#0F7B6C"` 같은 색 문자열. 파생 지표(Duration 등) 강조용. 비우거나 해석 불가하면 기본색. 어두운 테마 대응이 필요하면 `ModernTheme.IsDarkBased`로 밝은/진한 톤을 고른다 |
| `TextSemiBold` | Text 셀 전용 SemiBold 강조 (기본 false) — `TextColor`와 조합해 색+굵기 강조. AutoFitColumns 측정도 SemiBold 폭 기준 |

### 컬럼 종류 (Kind) — 그리드 전용

| Kind | 설명 | 함께 쓰는 속성 |
|---|---|---|
| `CheckBox` | bool 컬럼 양방향 체크박스 — 벌크 작업 대상 지정용. 읽기 전용 그리드에서도 클릭 한 번으로 토글되고 원본 행 값이 즉시 갱신된다. 비주얼은 ModernCheckBox와 동일한 모던 체크(둥근 사각 + 액센트 채움 + 흰 체크 글리프) | `HeaderCheckBox` — true면 헤더에 **전체 선택/해제 체크박스** 표시 (기본 false). 클릭 시 현재 표시 중인 모든 행 일괄 설정, 상태는 전체(체크)/일부(대시)/없음(해제)을 되비춘다 |
| `Badge` | 값을 색 배지로 표시 (모양은 `BadgeShape` — `Rounded` 둥근 사각 기본 / `Pill` 알약). 글자색은 배경색에서 자동 유도. **같은 Badge 컬럼의 배지는 가장 긴 표시값 기준으로 같은 폭**을 사용하고, 폭을 적지 않으면 그 폭이 **컬럼 고정 폭**이 된다(2026-09-07 — 별 너비로 늘어나지 않는다). `BadgeWidthValues`에 올 수 있는 값 목록을 주면 들어온 데이터가 아니라 그 어휘로 재고, `BadgeWidthByValue = true`면 통일을 꺼 배지마다 제 값 길이로 그린다 | 색 지정 4경로 (우선순위: 명시적일수록 우선, 하나만 적용): ① `BadgeColorMember` — 배경색(`"#FEE2E2"` 등) 컬럼 이름 (완전 수동; 색이 비면 일반 텍스트). ② `BadgeSemanticMember` — 의미 어휘 컬럼 (`"Y"`/`"N"`/`"success"`/`"warning"`/`"error"`/`"info"`/`"neutral"` → `SemanticColors` 표준색; 모르는 어휘는 Neutral, 예외 없음). ③ `BadgeGroupMember` — 그룹 키 컬럼 (쿼리가 CASE WHEN으로 판정해 내려주는 표준 경로 — **정수면 Palette 인덱스 직결**(상호 구분 보장), 문자열 키면 `FromValue` 유도). ④ `BadgeAutoColor` — `true`면 **셀 값 자체에서 자동 유도** (`Palette.FromValue` — 같은 값 = 항상 같은 색이고, 해시가 겹친 값은 자동으로 다른 색에 벌려 배치된다(2026-08-12); 색 컬럼·색 배열 불필요). 스핀: `BadgeSpinBorderMember`(판정 컬럼 — 참인 행만 배지 테두리에 **옅은 베이스 링 + 좌→우로 훑는 코멧 빛띠**) 또는 `BadgeSpinValues`(**값 선언형** — `"SENDING;PROC"`처럼 배지 값 목록을 선언하면 그 값인 행만 회전; 더미 판정 컬럼 불필요, 정규화는 FromValue와 동일. SpinBorderMember가 있으면 그쪽 우선). `BadgeShape`(선택) — 배지 모양(기본 `ChipShape.Rounded`). **강조**: `BadgeAccentValues`(2026-08-09 추가, 값 목록 세미콜론/쉼표 구분) — 그 값의 배지만 **진한 채움 + 테두리**로 바꾼다(글자 굵기는 평소와 같다). 위 색 4경로보다 앞선다. 글자만 빨갛게 하지 않는 이유는 대비다 — 자동색 배경은 값마다 달라서 그 위 빨간 글자가 테마 10종 중 9종에서 4.5:1 미달이고 일부는 배경과 같은 색이 된다. 진한 채움은 전 테마 통과(최저 4.63). 색만으로는 HighContrast·CrimsonGray에서 일반 칩과 겹칠 수 있어 **테두리**가 함께 켜진다 (컨트롤 계약 검사가 지킨다) |
| `Button` | 행 단위 액션 버튼 — ModernButton Secondary와 같은 문법(평상시 흰 배경 + 회색 테두리 + 진한 글자, hover 시 옅은 파랑 틴트 + 액센트 테두리/글자, pressed 시 한 단계 진한 틴트). 반복되는 행 액션에 맞춰 캡션은 Label 크기의 일반 굵기로 표시한다. 클릭 시 그리드의 `CellButtonClick` 발생 | `ButtonText` — 캡션. `ButtonEnabledMember` — 행별 활성 여부 컬럼(bool 또는 `"Y"`/`"true"`/`"1"`; 비우면 항상 활성). `ButtonTextMember`(2026-08-09 추가) — **행마다 캡션이 달라지는** 컬럼 이름. 한 행이 단계 하나만 가질 때 액션 컬럼을 여러 개 두지 않기 위한 것이다(예: Accept / Receive / Create 중 그 행의 단계 하나). **값이 비면 그 행에는 버튼을 그리지 않는다** — 비활성 버튼이 아니라 아예 없다. AutoFit 폭도 그 컬럼 값 중 가장 긴 캡션 기준으로 잰다. **버튼 자체 폭도 가장 긴 캡션 기준으로 통일된다**(2026-08-13 추가 — 배지 폭 통일과 같은 규칙; 캡션 길이가 달라도 같은 컬럼의 버튼은 같은 폭). `ButtonAutoColor`(2026-08-12 추가) — `true`면 **캡션 값 자체에서 색을 자동 유도**한다 (배지 `BadgeAutoColor`와 같은 `Palette.FromValue` 규칙 — 같은 캡션 = 항상 같은 색·겹치는 색 자동 회피, 공백/대소문자 무시). **배경은 중립 Secondary 버튼과 같은 표면 그대로**(2026-08-13 변경 — 칩 채움은 너무 강렬했고 틴트는 흰색도 색도 아닌 애매한 면이었다)이고, 색 구분은 **칩 색 계열의 글자·테두리**가 담당한다(대비 4.5:1 보장). **hover에서 그 값의 배지 칩(파스텔)이 채워지고** pressed는 한 단계 짙어져 상호작용 문법을 유지한다. `ButtonTextMember`와 조합하면 행마다(예: Accept/Receive/Create) 색이 갈리고, hover 색이 같은 값의 배지와 정확히 같다. 캡션이 빈 행/해석 불가 값은 중립 버튼 색, 비활성 행은 기존 회색 처리 그대로 |
| `Combo` | 셀 콤보 입력(양방향) — 고정 선택지 중 하나를 고르면 원본 행 값이 즉시 갱신. 읽기 전용 그리드에서도 동작. 판정/등급 입력용 | `ComboItems`(공통 선택지 `string[]`) 또는 `ComboItemsMember`(**행별** 선택지 — 그 컬럼 셀의 `string[]`, 행마다 범위 다를 때). `ComboEnabledMember`(행별 활성). `ComboItemColors`(선택지별 배지색) |
| `Spinner` | 인디터미네이트 **발광 바**(액센트 코멧 — 머리가 짙고 꼬리는 투명하게 사라지는 띠가 알약 트랙 위를 좌→우로 흐름 + 글로우) — "그 행이 작업/처리 중"을 셀 안에서 보여 준다. 값이 아니라 상태만 표시(진행률 없음), 정렬 대상 아님 | `SpinnerActiveMember` — 참인 행만 스피너가 동작(bool 또는 `"Y"`/`"true"`/`"1"`; 비우면 모든 행) |
| `Link` | 값 텍스트를 **액센트색 하이퍼링크**(항상 밑줄, 호버 시 진한 액센트, 손 커서)로 표시 — "Lot ID 클릭 → 이력 화면" 관례용. 클릭 시 그 행이 먼저 현재 행으로 선택된 뒤 그리드의 `CellLinkClick` 발생. 정렬/값 필터/복사/자동 폭은 텍스트 셀과 동일, 값이 빈 행은 빈 셀 | (별도 속성 없음 — `Format`/`TextAlignment`는 텍스트 셀과 동일하게 적용) |

주의: 컬럼 필터 결과나 페이지 조각처럼 **복사본 DataTable**을 바인딩하는 화면은
체크 변경을 원본에 되돌리는 동기화가 필요하다 (`ColumnChanged` 구독 — Samples의
LogisticsRequestForm 참고).

```csharp
// 원하는 개수만큼 나열
new ModernDataGridColumn("CHK", "", 44) { Kind = GridColumnKind.CheckBox },
new ModernDataGridColumn("EMP_NO", "사번", 90),
new ModernDataGridColumn("SALARY", "급여", 100) { TextAlignment = GridTextAlignment.Right, Format = "N0" },
new ModernDataGridColumn("ELAPSED_DAYS", "Days", 70)
    { Kind = GridColumnKind.Badge, BadgeColorMember = "DAYS_COLOR", TextAlignment = GridTextAlignment.Center },
new ModernDataGridColumn("REQ_ACTION", "Request", 100)
    { Kind = GridColumnKind.Button, ButtonText = "Create", ButtonEnabledMember = "REQ_CAN" },
new ModernDataGridColumn("NOTE", "비고")   // 마지막은 폭 생략으로 채움
```

---

## 테마 (ModernTheme) — 라이트/다크 + 틴트·고대비·Solarized·Nord 변형

전 컨트롤 공통의 테마 (`Modern.Lab.Theming.ModernTheme`). 기본은 라이트이며,
**앱 시작 시 첫 컨트롤을 만들기 전에 한 번** `Mode`를 설정하는 opt-in 방식이다 —
설정하지 않으면 기존과 완전히 동일하므로, 이 라이브러리를 쓰는 다른 시스템에는
영향이 없다.

| 테마 (`ThemeMode`) | 특징 |
|---|---|
| `Light` (기본) | 연한 라이트 Fluent — 블루 그레이 뉴트럴 + 블루 액센트 `#0078D4` |
| `Dark` | 어두운 배경 전체 반전 |
| `OrangeBlue` | 웜 오렌지 액센트 `#CA5010` + 웜 오렌지 **파스텔** 배경/테두리, 선택 강조는 블루 파스텔 (GreenTomato와 같은 구조) |
| `GreenTomato` | 딥 그린 액센트 `#217346` + 민트 **파스텔** 배경/테두리, 선택 강조는 토마토 파스텔 |
| `CrimsonGray` | **미드 그레이 모노톤** (다크 계열 dim — Dark보다 한 톤 밝음) + 라이트 크림슨 액센트 `#F2919E` |
| `LightPurple` | Fluent 퍼플 액센트 `#5C2E91` + 라벤더 **파스텔** 배경/테두리 |
| `HighContrast` | **접근성/현장 시인성 고대비** — 순검정 면 + 순백 텍스트/테두리(면 톤 대신 테두리로 구획 구분), 밝은 시안 액센트 `#6FE0FF`(검정 글자), 시맨틱은 검정 위 고채도 밝은 색(초록 `#3FF23F`/노랑 `#FFD400`/빨강 `#FF5050`). 나쁜 조명·먼 거리·저시력 환경 기준 |
| `SolarizedLight` | **Solarized Light 정본** — 크림 카드 `#FDF6E3`(base3) + 크림 배경 `#EEE8D5`(base2), 블루 액센트 `#1A6598`(정본 `#268BD2`는 크림 글자와 3.4:1이라 채움 버튼 대비 확보용으로 반 톤 어둡게), 시맨틱은 Solarized 원색(green `#859900`/yellow `#B58900`/red `#DC322F`). 저채도·저대비 설계라 장시간 모니터링에 눈이 편하다 |
| `SolarizedDark` | **Solarized Dark 정본** — 딥 틸 배경 `#002B36`(base03) + 카드 `#073642`(base02), 밝힌 블루 액센트 `#4BA3DE` + base03 OnAccent(Win11 Dark 문법 — 채움 버튼 대비 확보), 텍스트는 base1 계열 |
| `Nord` | **Nord 정본** (nordtheme.com) — Polar Night 청회색 면(`#2E3440`~`#4C566A`) + Snow Storm 텍스트 + Frost 시안 액센트 `#88C0D0`, 시맨틱은 Aurora(red `#BF616A`/yellow `#EBCB8B`/green `#A3BE8C`). Dark보다 덜 차가운 다크 |

파스텔 변형(OrangeBlue/GreenTomato/LightPurple)과 SolarizedLight는 라이트
기반이다: 배경·테두리·선택 강조·액센트가 각 테마 톤으로 바뀐다. 카드 표면은
파스텔에서 흰색, SolarizedLight에서는 크림(`#FDF6E3`, 순백 없음)이다.
텍스트·시맨틱(성공/경고/오류) 색은 파스텔에서는 고정된 Win11 순정 값,
SolarizedLight에서는 Solarized 원색이다. Dark·CrimsonGray·HighContrast·
SolarizedDark·Nord는 어두운 계열이라 텍스트/시맨틱까지 어두운 면 기준으로
재정의된다. 적용 방법은 모두 동일하다 (`Mode` 설정 + 화면당 `Apply(this)`).

주의: 그리드 배지색처럼 **화면이 데이터로 넣는 색**(`STATUS_COLOR` 등
`*ColorMember` 계약)은 테마가 덮지 못한다 — 테마와 한 식구인 색을 원하면
화면이 색을 만들 때 아래 `Palette`(종류색)/`SemanticColors`(의미색)를 쓰면
된다 (테마별 레시피가 자동 적용된다).

이진 호환: `ThemeMode`의 번호는 명시적으로 고정돼 있고 **5(구 Blue)·7(구 Mono)은
영구 결번**이다(2026-08-07 테마 삭제). enum 상수는 소비자 바이너리에 숫자로
박히므로 번호를 재사용하면 DLL 교체만 한 옛 프로그램이 조용히 다른 테마로
오동작한다 — 새 테마는 반드시 마지막 번호 뒤에 붙인다. 삭제된 값이 들어오면
모든 소비 지점이 Light로 저하한다.

### 범주 색 팔레트 (Palette)

종류 구분용 색을 폼마다 손으로 고르지 않아도 되는 생성기
(`Modern.Lab.Theming.Palette`). 테마와 종류 수를 주면 그 테마에 어울리는
배경색 문자열(`"#RRGGBB"`) 배열을 돌려준다 — 배지/칩 배경이 대표 용도로,
그리드/트리 배지의 `*ColorMember` 값, `ModernStatusBadge.Color`에 그대로 넣는
형식이고, 글자색은 각 컨트롤이 배경색에서 자동 유도하므로 배경 배열만 있으면
된다. 색으로 종류를 구분하는 곳(범례, 태그, 시리즈 등)이면 어디든 쓸 수 있다.

| 멤버 | 설명 |
|---|---|
| `FromValue(string value)` / `FromValue(mode, value)` | **값 유도 자동색 — 값 하나에서 색 하나** ("같은 값 = 같은 색, 다른 값 = 다른 색"). 정규화(공백/대소문자 무시)한 값의 결정적 해시로 색상 슬롯(12개)을 정한다. **색 자체는 두 경로**(2026-08-13 변경): 라이트 계열 테마는 색상마다 채도·명도를 따로 보정한 **큐레이션 파스텔 표**(12색 × 톤 3 — 초기 화면들이 하드코딩하던 #DBEAFE·#DCFCE7 계열; HSL 공식 하나로 찍으면 노랑·연두가 탁해지는 지각 비균일 때문), 다크 계열·Solarized는 테마 레시피 공식. 그리드/트리 `BadgeAutoColor`·`ModernStatusBadge.ColorValue`가 이 함수를 쓰므로, 직접 불러도 배지들과 정확히 같은 색을 얻는다. 빈 값/null은 중립 배지색. **충돌 자동 회피** (2026-08-12 변경 — 그전에는 해시가 같은 슬롯에 겹친 두 값이 미세한 톤 차이뿐인 "거의 같은 색"이 될 수 있었다): 프로세스 전역 배정 레지스트리가 이미 차지된 슬롯을 기억해, 겹친 값을 빈 슬롯(색상 우선, 12색상 소진 후에는 뚜렷한 톤 티어)으로 벌려 배치한다. 먼저 만난 36종(색상 12 × 톤 3)까지는 서로 다른 값이 같은 색을 받지 않으며, 그 뒤 값은 예전 규칙(해시+톤 지터)으로 폴백한다. 배정은 한 실행 안에서 전 화면이 공유하고, 그리드/트리가 바인딩 시점에 고유 값을 정렬 등록하므로 같은 데이터 도메인이면 실행이 바뀌어도 같은 색이 나온다 |
| `GetColors(int count)` | 현재 테마(`ModernTheme.Mode`) 기준으로 count개 — 인덱스 배정형(상호 구분 보장, prefix 안정) |
| `GetColors(ThemeMode mode, int count)` | 지정 테마 기준 |
| `GetPastelColors(int count)` | **테마와 무관하게 항상 같은 파스텔 배열** (Light 테마 레시피 고정). 테마가 무엇이든 색을 고정하고 싶은 화면용 |
| `NormalizeValue(string)` | `FromValue`·`BadgeSpinValues`가 쓰는 값 정규화 (트림 + 대문자화, 문화권 불변) — 화면이 같은 규칙으로 값을 비교할 때 사용 |
| `RegisterValues(IEnumerable<string>)` | 값들을 색 배정 레지스트리에 **정렬 순서로 일괄 선등록** — 그리드/트리가 바인딩 시점에 자동 호출하므로 화면이 부를 일은 거의 없다. 데이터에서 모은(순서가 우연인) 값 목록용 |

**FromValue와 GetColors 중 무엇을 쓰나**: 종류 목록을 선언하지 않고 값에서 바로
색을 얻으려면 `FromValue`(대부분의 상태/종류 배지 — 그리드에서는
`BadgeAutoColor = true`가 이것이다), 서로 확실히 달라 보여야 하는 소수의 범주를
직접 배정하려면 `GetColors` 인덱스(그리드에서는 쿼리 그룹 번호 +
`BadgeGroupMember`)를 쓴다. `FromValue`도 먼저 만난 36종까지는 상호 구분을
보장하므로, 상태/단계처럼 종류가 한 자릿수인 값 컬럼은 자동색으로 충분하다.

테마를 이름 문자열로 받는 오버로드는 두지 않는다 — 설정 문자열은 화면이
`Enum.TryParse`로 해석해서 enum으로 넘긴다 (오타가 조용히 폴백되는 것보다
해석 실패가 화면에 드러나는 편이 안전하다).

색 배정 규칙:

- **색 배정** — 테마 액센트의 색상(Hue)에서 시작해 황금각(137.508°)씩 회전한다.
  연속 색이 서로 최대한 벌어지고, **종류 수가 늘어도 앞쪽 색은 바뀌지 않는다**
  (상태 종류가 추가돼도 기존 상태의 색이 유지된다). 채도/명도는 테마 성격을 따른다 —
  라이트 계열은 파스텔(SolarizedLight는 뮤트 파스텔), Dark·SolarizedDark·Nord는
  어두운 칩, CrimsonGray는 뮤트한 밝은 칩, HighContrast는 검정 위 밝은 고채도 칩.
- 정상=초록/오류=빨강처럼 **의미가 정해진 상태색은 이 팔레트가 아니라 아래
  `SemanticColors`를 쓸 것** — 이것은 색에 의미를 싣지 않는 "종류 구분"용 범주
  팔레트다.

사용상 주의 두 가지:

- **색 고정은 인덱스 기준이다** — 팔레트가 보장하는 것은 "i번째 색 불변"(prefix
  안정)이지 "특정 값의 색 불변"이 아니다. 값→색 연결은 화면이 만드는데, 종류
  목록의 순서가 바뀌면(예: 정렬된 목록의 중간에 새 값이 끼어들면) 그 뒤 값들의
  색이 전부 한 칸씩 밀린다. 재조회에도 값별 색을 유지하려면 종류 목록을 고정
  순서로 선언하거나, 동적으로 모을 때는 **뒤에만 추가(append)** 할 것.
- **8종 이하에서 쓸 것** — 색상(Hue) 회전만으로 구분하므로 종류가 8개를 넘으면
  새 색이 기존 색 근처에 떨어져 구분력이 조용히 무너진다. 또한 색상만의 구분은
  적록색약 사용자에게 취약하다 — 색은 보조 수단으로 쓰고, 배지 **텍스트가 항상
  값을 말하게** 할 것 (텍스트 없이 색만으로 종류를 표현하는 설계 금지).

```csharp
// 조회 결과의 상태 종류 수만큼 색을 받아 행의 STATUS_COLOR에 내려 준다.
List<string> kinds = new List<string> { "Run", "Idle", "Done", "Down" };
string[] palette = Modern.Lab.Theming.Palette.GetColors(kinds.Count);

foreach (DataRow row in table.Rows)
{
    row["STATUS_COLOR"] = palette[kinds.IndexOf((string)row["STATUS"])];
}
```

### 의미 상태색 (SemanticColors)

정상=초록/오류=빨강처럼 **의미가 정해진** 상태색의 공통 API
(`Modern.Lab.Theming.SemanticColors`) — 모든 개발자·화면이 같은 의미에 같은
색을 쓰게 한다. 반환 형식은 Palette와 같은 `"#RRGGBB"` 문자열이라
`*ColorMember`·`ModernStatusBadge.Color`에 그대로 들어간다.

**역할 분담(리뷰 기준): 의미가 있는 색은 `SemanticColors`, 의미 없이 종류만
구분하는 색은 `Palette` — 헥사 문자열을 화면 소스에 직접 쓰지 않는다.**

| 멤버 | 의미 | 사용례 |
|---|---|---|
| `Success` / `SuccessOf(mode)` | 정상·완료·가능 | Run, Completed, 통보됨(Y) |
| `Warning` / `WarningOf(mode)` | 주의·개입 중·대기 | Local 통신, 연결 지연, 투입됨 포트 |
| `Error` / `ErrorOf(mode)` | 불가·실패·위험 | Down, OffLine, 미통보(N), 1순위 |
| `Info` / `InfoOf(mode)` | 진행·정보 | 작업중 포트, Received |
| `Neutral` / `NeutralOf(mode)` | 무상태·해당없음 | Idle, 빈 포트 — 중립 배지 토큰(`NeutralBackground`)과 항상 같은 값 |
| `Priority(rank)` / `Priority(mode, rank)` | 우선순위 — 1=Error · 2~3=Warning · 그 외=Neutral | 대기 Lot 순번 배지 |
| `GetAgingRamp()` / `GetAgingRamp(mode)` | 경과/심각도 4단계 램프 — 파랑→호박→주황→빨강 | 경과일 배지 (0-2/3-6/7-13/14+일 구간) |
| `YesNo(bool)` / `YesNo(mode, bool)` | 이분 상태 — Y=Success / N=Error | 통보/사용 여부 배지 |
| `Of(SemanticKind)` / `Of(mode, kind)` | enum → 색 (None은 Neutral) | `ModernStatusBadge.Semantic`과 같은 어휘로 코드에서 색을 받을 때 |
| `FromName(string)` / `FromName(mode, name)` | **의미 어휘 문자열 → 색** — `"Y"`/`"YES"`/`"TRUE"`/`"1"`/`"SUCCESS"`/`"OK"`=Success, `"N"`/`"NO"`/`"FALSE"`/`"0"`/`"ERROR"`/`"FAIL"`/`"DANGER"`=Error, `"WARNING"`/`"WARN"`=Warning, `"INFO"`/`"PROGRESS"`=Info, 그 외(빈 값·모르는 어휘)=Neutral (예외 없음, 공백/대소문자 무관) | 그리드/트리 `BadgeSemanticMember`가 이 매핑을 쓴다 — 쿼리가 판정 어휘를 내려주면 폼 코드 0줄 |
| `KindFromName(string)` | 의미 어휘 → `SemanticKind` (FromName과 같은 어휘) | 화면이 어휘를 enum으로 해석할 때 |

테마 인식: 라이트 계열(Light·파스텔)은 관행 파스텔 틴트 그대로(기존 화면
외관 불변), SolarizedLight는 Solarized 원색 계열의 크림 틴트(상태 배너
배경 토큰과 같은 값 — 배지와 배너가 한 색 계열), Dark·SolarizedDark·Nord는
어두운 칩, CrimsonGray는 뮤트한 밝은 칩, HighContrast는 검정 위 밝은 고채도
칩이다. 유도 글자색은 전경-배경 대비가 4.5:1에 못 미치면 검정/흰색으로
자동 강등된다(중간톤 배경 방어).

**XAML 시맨틱 토큰과의 역할 구분**: `Brush.Success` 등 토큰은 다이얼로그
아이콘·배너 텍스트용 **전경/테두리 색**이고, `SemanticColors`는 배지/칩의
**배경색**이다 — 모든 테마에서 서로 다른 값이 정상이다(같은 색 계열, 다른
역할). 두 API가 같은 값을 반환해야 하는 관계가 아니다.

```csharp
// 상태 배지: 의미로 색을 받는다 — 테마를 바꿔도(Dark/Nord 등) 화면이 따라온다.
row["STATE_COLOR"] = down ? SemanticColors.Error
        : running ? SemanticColors.Success : SemanticColors.Neutral;
row["PRIO_COLOR"] = SemanticColors.Priority(rank);
row["DAYS_COLOR"] = SemanticColors.GetAgingRamp()[band];   // 0~3 구간
```

| 멤버 | 설명 |
|---|---|
| `ModernTheme.Mode` | 위 표의 값 중 하나 (진실 공급원은 `ThemeMode` enum). 시작 시 한 번만 설정 |
| `ModernTheme.IsDark` | Dark 여부 (읽기 전용) |
| `ModernTheme.IsDarkBased` | 어두운 표면 계열 여부 — Dark/CrimsonGray/HighContrast/SolarizedDark/Nord (다크 타이틀바 등 공통 처리 기준) |
| `Surface` / `Background` / `Border` / `TextPrimary` / `TextSecondary` / `Accent` / `SelectionBackground` / `SurfaceAlt` 등 | 중앙 팔레트 색(`System.Drawing.Color`) — 현재 테마에 맞는 값을 돌려주므로, 폼 배경 등 라이브러리 밖 요소를 칠할 때 사용 |

```csharp
// Program.cs — 반드시 첫 폼 생성(Application.Run) 전에
ModernTheme.ThemeMode mode;
if (!Enum.TryParse(settings.ThemeName, true, out mode))   // "dark", "nord", "solarizedlight", ...
{
    mode = ModernTheme.ThemeMode.Light;
}
ModernTheme.Mode = mode;
Application.Run(new MainForm());
```

동작 원리 (통합자는 몰라도 됨):

- WPF(ElementHost) 컨트롤 — Light가 아니면 `Tokens.<테마>.xaml`이 `Tokens.xaml`
  뒤에 병합돼 같은 토큰 키를 테마 값으로 덮는다 (다크는 전체, 틴트는 액센트/배경만).
- 순수 GDI+ 컨트롤(`ModernLabel`/`ModernStatusBadge`/`ModernCardPanel`/`ModernGroupBox`)
  — XAML을 읽을 수 없으므로 `ModernTheme` 팔레트 색을 직접 읽는다.

주의:

- **런타임 토글은 지원하지 않는다** — WPF StaticResource가 로드 시 확정되기 때문.
  테마 전환 UI는 설정을 저장한 뒤 **앱 재시작**으로 반영한다.
- 일반 WinForms 컨트롤(폼 배경, 기본 `Button`/`TextBox` 등)은 자동으로 어두워지지
  않는다 — 폼 쪽에서 `ModernTheme` 팔레트 색으로 직접 칠해야 한다.
- **카드 배경색은 디자이너에 직렬화되지 않는다** (v0.4.1) — VS 디자이너는 항상
  라이트 모드로 돌기 때문에, 과거에는 `ModernCardPanel`/`ModernGroupBox`의
  `BackColor`(흰색)가 `.Designer.cs`에 저장돼 다크 테마에서 카드가 라이트로
  남는 문제가 있었다. v0.4.1부터 `BackColor` 직렬화를 차단하고, 이미 저장돼 있는
  값도 런타임(핸들 생성 시점)에 테마 표면색으로 복구하므로 **기존 폼의
  `.Designer.cs`는 수정할 필요가 없다**.

### 테마 적용 체크리스트 (기존 앱 — 다크/틴트 공통)

1. **DLL 교체** — `Modern.Lab.Commons` v0.6.0 이상 (다크만이면 v0.5.0 이상).
2. **Program.cs** — `Application.Run(...)` **직전**(첫 폼 생성 전)에 한 번:
   ```csharp
   // Dark 자리에 OrangeBlue / GreenTomato / CrimsonGray / LightPurple / HighContrast / SolarizedLight / SolarizedDark / Nord를 넣으면 해당 테마
   Modern.Lab.Theming.ModernTheme.Mode = Modern.Lab.Theming.ModernTheme.ThemeMode.Dark;
   ```
3. **각 폼** — 생성자에서 `InitializeComponent()` **직후** 한 줄:
   ```csharp
   Modern.Lab.Theming.ModernThemeWinForms.Apply(this);
   ```
4. `.Designer.cs`는 손대지 않는다 — 옛 `BackColor` 직렬화 줄이 남아 있어도
   런타임에 복구/치환된다.

동작 확인은 샘플 갤러리로: `Modern.Lab.Samples.exe --dark` 또는 `--theme=nord` 등
(`--theme=`에는 `ThemeMode` 값 이름을 소문자로 넣는다).

### WPF 호스트 커서 방어 (v0.9.0) — WpfHostOptions / WpfHostCursorGuard

호스트 폼(수정할 수 없는 공용 base form 등)이 조회 중 `Cursor = WaitCursor` /
`UseWaitCursor = true`를 걸면 ElementHost의 기본 속성 매핑이 그 값을 WPF 콘텐츠로
**복사**하는데, 복원이 매핑에 반영되지 않는 경로(`Cursor.Current`로 복원, 비 UI
스레드 복원, 예외로 복원 누락)를 타면 **WPF 컨트롤 위에만 Wait 커서가 영구히
남는다** (네이티브 컨트롤·빈 배경은 정상으로 보이는 것이 특징). 이를 옵트인으로
차단한다:

```csharp
// 방법 A(권장) — Program.cs, 첫 폼 생성 전 한 줄. 모든 래퍼(동적 생성 포함) 커버.
Modern.Lab.WinForms.Controls.Hosting.WpfHostOptions.DisableCursorPropertyMap = true;

// 방법 B — 특정 폼만: InitializeComponent() 직후 (테마 Apply와 같은 자리).
Modern.Lab.WinForms.Controls.Hosting.WpfHostCursorGuard.Apply(this);
```

- 켜면 WPF 콘텐츠가 항상 자기 커서를 관리하므로 잔류가 원천 차단된다. 이미 Wait가
  박힌 화면에 B를 적용해도 다음 마우스 이동부터 정상으로 돌아온다.
- 트레이드오프: 폼이 **의도적으로** 건 Wait 커서도 WPF 컨트롤 위에서는 보이지
  않는다 (커서 표시 외에 기능·데이터·이벤트 영향은 없음).
- 기본값 off — 켜지 않으면 기존 버전과 완전히 동일하게 동작한다.
- 샘플 확인: `Modern.Lab.Samples.exe --cursor-guard`

### WPF 소프트웨어 렌더링 (2026-09-02) — WpfHostOptions.SoftwareRendering

ElementHost 섬은 하나마다 GPU 렌더 타깃을 만든다. 그 생성이 PC·그래픽 드라이버에 따라 **섬당 ~100ms**까지
들어(2026-09-02 실측 — 빈 `Border` 하나짜리 순정 `ElementHost`도 같다), 표 5개·KPI 카드 4장·콤보 2개인 화면은
뜨는 데만 3초가 넘었다. 소프트웨어 렌더링으로 고정하면 섬당 ~7~10ms다.

```csharp
// Program.cs, 첫 폼 생성 전 한 줄.
Modern.Lab.WinForms.Controls.Hosting.WpfHostOptions.SoftwareRendering = true;
```

- 업무 화면(애니메이션·효과 없음)은 그림이 같다. RDP·가상 데스크톱은 원래 소프트웨어로 그린다.
- 트레이드오프: 매우 큰 표의 스크롤을 GPU 대신 CPU가 그린다.
- 기본값 off — 켜지 않으면 기존 버전과 완전히 동일하게 동작한다. 샘플·갤러리는 켜서 돌린다
  (`--hw-render`로 끄고 비교할 수 있다). 섬 단가 측정: `Modern.Lab.Samples.exe --hw-render --diag-hostcost`
  → `%TEMP%\hostcost-diag.txt`.

### ModernThemeWinForms.Apply(root) — 화면 테마 적용 헬퍼 (v0.5.0)

`Modern.Lab.Theming.ModernThemeWinForms`. Light가 아닌 테마(다크/틴트)일 때만
동작하고 기본 라이트에서는 완전한 no-op이므로 조건문 없이 항상 호출해도 안전하다.
치환 결과는 현재 테마의 팔레트 값 — 아래 표의 "다크" 열은 Dark 기준 예시이고,
틴트 테마에서는 같은 규칙으로 그 테마의 색이 들어간다.

`Apply(Control root)` — **root는 Form이 아니어도 된다.** 화면이 UserControl이나
사내/서드파티 프레임워크의 베이스 컨트롤이면 그 루트를 그대로 넘긴다. root가
Form이 아니면 타이틀바는 건드리지 않으므로, 최상위 폼에서
`ApplyDarkTitleBar(mainForm)`을 한 번 따로 호출한다.

| 하는 일 | 내용 |
|---|---|
| 타이틀바 | root가 Form일 때만 — OS 타이틀바를 다크로 (DWM immersive dark mode, Win10 1809+; 미지원 OS는 조용히 무시) |
| 루트 배경 | `ModernTheme.Background`로 설정 |
| 자식 컨트롤 | 전체 재귀 순회하며 아래 표의 "알려진 라이트 색"과 **정확히 일치**하는 `BackColor`/`ForeColor`만 다크 팔레트로 치환 — 상태색(빨강/초록 등) 등 의도적인 색은 보존 |

색 치환 표:

| 하드코딩돼 있던 라이트 색 | 치환되는 다크 팔레트 |
|---|---|
| `Color.White` (255,255,255) | `Surface` |
| (249,250,251) / (247,248,250) | `SurfaceAlt` |
| (243,244,246) / `SystemColors.Control` | `Background` |
| (17,24,39) / `SystemColors.ControlText` | `TextPrimary` |
| (107,114,128) | `TextSecondary` |
| (55,65,81) | `NeutralText` |

- WPF(ElementHost) 컨트롤은 건너뛴다 — `Tokens.Dark.xaml`이 스스로 처리.
- 런타임에 동적으로 추가한 컨트롤은 추가 후 `Apply(root)`를 다시 호출하면 된다
  (화면 생성 시 1회 호출 기준으로 설계 — 반복 호출을 전제로 하지는 말 것).
- **커스텀 페인트에는 닿지 않는다** — `Paint`/`CellPaint` 핸들러 안에서 색을
  하드코딩해 직접 그리는 코드는 Apply가 바꿀 수 없다. 그런 코드는
  `ModernTheme` 팔레트 색으로 그리도록 고친다 (예: 샘플
  `LotHistoryForm.OnDetailCellPaint` — 캡션 `SurfaceAlt`, 괘선 `BorderSubtle`).
- 타이틀바만 필요하면 `ModernThemeWinForms.ApplyDarkTitleBar(form)` 개별 사용 가능.
- `ModernSpreadGrid`(FarPoint COM)도 셀/헤더/선택/교차색을 `ModernTheme` 팔레트에서
  읽으므로 전 테마가 모두 적용된다 (앱 시작 시 `Mode` 설정 기준 — 회사 PC에서
  interop 연결 후 동작 확인 필요).

---


## 문구 작성 기준

컨트롤을 어떻게 쓰는지와 별개로, **그 안에 들어가는 말**도 기준이 필요하다.
색·간격에 토큰이 있는 것과 같은 이유다 — 사람마다 다르게 쓰면 화면마다 어긋난다.

> **[변환 작업에서는 문구를 바꾸지 않는다.]** 기존 폼을 변환할 때는 화면에 있던
> 문구를 그대로 옮긴다. 문구를 고치는 것은 업무 표현을 바꾸는 일이라 사람의 판단이
> 필요하다(변환 절차서 절대 규칙 2). 아래 기준은 **새 화면·새 문구를 작성할 때**의
> 것이다.

### 1. 이름은 사용자가 인식하는 말로

시스템 구조가 아니라 사용자가 다루는 대상의 이름을 쓴다.

| 쓰지 않음 | 씀 |
|---|---|
| `IF_REQ_MAS 처리` | `의뢰 처리` |
| `RECV_YN 갱신` | `수신 확인` |
| `전문 전송` | `저장` |

컬럼 캡션은 예외다 — 현업이 컬럼명을 그대로 부르는 경우가 많으므로
`GridCaptionDictionary`의 회사 표준 용어를 따른다.

### 2. 액션은 흐름 전체에서 같은 단어

버튼이 `Receive`면 결과 토스트도 `Received`다. 버튼은 "수신"인데 토스트가
"등록되었습니다"이면 사용자가 같은 동작인지 확신할 수 없다. 확인 대화상자·
토스트·로딩 문구가 모두 그 버튼의 단어를 쓴다.

### 3. 실패는 사과하지 말고 원인과 다음 행동을 말한다

에러 문구는 시스템의 목소리로 **무엇이 잘못됐는지 + 어떻게 하면 되는지**를 말한다.
사과("죄송합니다")나 모호한 표현("오류가 발생했습니다")은 쓰지 않는다.

| 쓰지 않음 | 씀 |
|---|---|
| 오류가 발생했습니다 | 대상 캐리어에 빈 자리가 3개뿐입니다 (선택 5개) |
| 처리에 실패했습니다 | 이미 수신된 LOT입니다. 목록을 새로 조회하세요 |

서버가 사유를 돌려주면 그 문구를 그대로 보여 준다(`DataActionResult.Message`).

### 4. 빈 상태는 다음 행동을 알려준다

`EmptyText`는 "없음"이 아니라 **왜 비어 있고 무엇을 하면 되는지**를 말한다.

- 조회 전: `품번으로 조회하세요`
- 조회 결과 0건: `데이터가 없습니다`(기본값) 또는 `조건에 맞는 LOT이 없습니다`

### 5. 로딩 문구는 구체적인 동사로

Microsoft 지침도 범용 표현보다 **구체적인 동사**를 우선한다.
컨트롤 기본값(`ModernBusyOverlay.Message` = `"Processing..."`)은 폴백일 뿐이며,
화면은 무엇을 하는 중인지 넘긴다 — `Loading history...` / `Searching requests...`
/ `Saving...`.

조회에는 `Loading…`/`Searching…`, 저장·이동·삭제에는 `Processing…`/`Saving…`처럼
동작에 맞는 말을 쓴다. 저장 중에 `Loading...`이 뜨면 틀린 안내다.

### 6. 표기 언어는 화면 단위로 통일한다

한 화면 안에서 언어가 섞이면 안 된다 — 화면이 영어면 로딩 커버·오버레이·토스트·
빈 상태까지 영어로, 한국어면 전부 한국어로 맞춘다. 실제로 이 저장소에서
**영어 화면의 오버레이 문구만 한국어**여서 로딩 순간에 언어가 튀는 문제가 있었고,
같은 이유로 그리드 상태바와 페이지 바가 한 화면 아래쪽에 나란히 놓이는데 서로 다른
언어로 나와 어긋나 보인 적이 있다(지금은 둘 다 영어 기본).

**컨트롤 기본 문구는 영어다** (`"No data"`, `"{0:N0} rows"`, `"{0:N0} total"`,
디자이너 자리표시 `"Button"`/`"Label"` 등). 화면 대부분이 영어라 기본값을 영어로 두면
아무것도 지정하지 않아도 언어가 섞이지 않는다. **한국어 화면은 그 화면에서 문구를
지정**한다(예: 한국어 화면이면 `EmptyText = "데이터가 없습니다"`,
`TotalCountFormat = "총 {0:N0}건"`).

예외: 샘플의 의뢰서 정보 다이얼로그(`RequestInfoDialogForm` — 구 `ModernRequestInfo`
카드)는 라벨이 전부 한국어로 고정이다.
그리드 컬럼 캡션은 `GridCaptionDictionary`가 영/한 전환 사전이라
(`Apply(GridCaptionLanguage.Korean)`) 한국어 세트를 제공한다.

컨트롤 기본값은 언어 판단을 대신해 주지 않는다. 화면이 문구를 넘기는 것이 원칙이고,
기본값은 넘기지 않았을 때의 폴백이다.

### 7. 문장 형태

- 문장 끝 마침표는 문장형에만(`이미 수신된 LOT입니다.`), 라벨·버튼·캡션에는 쓰지 않는다
- 버튼·라벨은 짧은 명사구 또는 동사 원형(`저장`, `조회`, `Receive`)
- 진행 중을 나타내는 말줄임표는 `...`(마침표 3개)이 아니라 컨트롤 기본값과 같은
  형태로 통일한다 — 기존 문구를 복사해 쓰는 것이 가장 안전하다
## 컬럼·필드를 손으로 적지 않기 — `GridColumns` · `FieldDefinitions`

표에 무엇이 보일지는 **조회가 정한다.** 화면은 결과를 그대로 싣고, 쿼리 결과만으로는
알 수 없는 것만 덧붙인다 — 숨길 칸, 텍스트가 아닌 셀(배지·링크·체크), 문맥에 따라
달라지는 캡션. 컬럼을 늘리거나 줄이거나 순서를 바꾸는 일은 **쿼리에서** 한다.

```csharp
GridColumns.Of(ports)
        .Caption("PORT_TYPE", "Type")     // 표 제목이 이미 "Port List"라 여기서는 Type
        .Badge("PORT_TYPE", "MODE", "STATUS")
        .Spin("STATUS", "Run")            // 자기 값이 Run이면 배지가 돈다
        .BadgeSpin("PRIORITY", "RUN_YN")  // 도는 이유가 다른 칸에 있을 때
        .Bind(this.gridPorts);            // 컬럼 + DataSource 한 번에

FieldDefinitions.Of(lots)
        .Link("REQ_SERIAL_NO")
        .Span("DESCRIPTION", 5)
        .Apply(this.fieldInfo);
```

**적지 않아도 되는 것** — 캡션(용어사전이 `LOT_ID` → "Lot Id"로 푼다), 너비
(`AutoFitColumns`), 이름이 `_COLOR`로 끝나는 보조 컬럼(자동 제외), 이름이 `_TM`으로
끝나는 시각 컬럼(표준 표기 + 가운데 정렬).

| 메서드 | 무엇 |
|---|---|
| `Of(table)` / `Of(row)` | 조회 결과에서 컬럼(필드) 목록을 만든다 |
| `Only(...)` / `Hide(...)` | 목록에 남길 칸 / 뺄 칸 |
| `Caption(name, text)` | 문맥 캡션 — 용어사전과 다르게 부를 때만 |
| `Badge(...)` / `BadgeColor(name, member)` | 값에서 색이 나오는 배지 / 색을 다른 컬럼에서 받는 배지 |
| `Spin(name, values)` / `BadgeSpin(name, activeMember)` | 배지가 도는 조건 — **자기 값**이 그 목록에 있을 때 / **다른 컬럼**이 참인 행일 때. 값 어휘가 열려 있어 값으로 켤 수 없는 배지(순위처럼)는 뒤엣것을 쓴다 |
| `Link(...)` | 클릭 가능한 값 |
| `YesNo(name, enabledMember)` / `SelectBox(name, locked)` | 서버가 준 Y/N 체크 / 사람이 고르는 체크(표 사본에 만들어 붙인다) |
| `Center(...)` / `Right(...)` / `Width(name, w)` / `Format(name, f)` / `Time(...)` | 정렬·폭·표기 |
| `First(...)` | 맨 앞에 둘 칸 — 나머지는 조회 순서 그대로 |
| `Apply(grid)` / `Apply(tree)` / `Apply(list)` | 컬럼(필드)만 싣는다 |
| `Bind(grid)` | 컬럼과 데이터를 한 번에 — 둘의 순서를 틀릴 일이 없다 |

없는 컬럼 이름은 **조용히 무시한다** — 쿼리가 바뀌어도 화면이 깨지지 않는다.
같은 컬럼 구성으로 다시 부르면 컬럼을 재구성하지 않는다(값 필터·찾기 상태가 풀리지 않는다).

> 컬럼을 아예 지정하지 않아도 된다 — `DataSource`만 주면 `AutoColumns` 규칙으로
> 조회 결과가 그대로 표가 된다. 위 빌더는 거기에 화면 사정을 얹는 자리다.

## 부록 — Commons 비컨트롤 구획 (Captions / Data)

`Modern.Lab.Commons`는 컨트롤 라이브러리이지만, 회사 모든 프로젝트가 참조하는
단일 DLL이라는 점을 살려 **비UI 구획 두 개**를 함께 배포한다. 두 구획은 성격이
달라 편집 규칙도 다르다 — 이 구분이 "공용 프로젝트 잡동사니화"를 막는 방어선이다.

| 구획 (네임스페이스) | 성격 | 누가 편집하나 |
|---|---|---|
| `Captions/` (`Modern.Lab.Captions`) | **용어 메커니즘** — 맵 계열 용어사전(`MapCaptionDictionary`)의 키 상수·Resolve·`Apply(DataTable)` 로더. **용어 내용물은 DLL에 없다** — 운영 원본은 DB(`UI_TERM_DIC`), 그리드 용어집(`GridCaptionDictionary`)은 상위 Common 소스 `Modern.Lab.Hosting/Dictionaries/`(2026-08-01 호스트 앱으로, 2026-09-13 상위 Common으로 이동) | 아무도 — 용어 수정은 DB에서 (DLL 재배포 없음) |
| `Data/` (`Modern.Lab.Data`) | **기계 유틸** — 화면·도메인 무관 DataTable 도구 | 아무도 (편집할 일이 없어야 정상) |

### Modern.Lab.Data.TableHelper

폼/프레젠터가 서버 조회 결과(DataTable)를 안전하게 읽는 최소 유틸:

| 멤버 | 설명 |
|---|---|
| `CellText(DataRow, string)` / `CellText(DataRowView, string)` | 셀 값을 안전하게 문자열로 — 컬럼 자체가 없거나(서버 JSON의 null 키 생략) DBNull이면 빈 문자열 |
| `ParseInt(string)` | 서버 숫자 컬럼(JSON number)을 관용적으로 정수 파싱 — 빈 값/소수 표기 허용, 실패 시 0 |
| `FlagSet(DataRow, string)` | bool 파생 컬럼(체크박스/버튼 활성 플래그) 판정 — 컬럼 없음/비bool이면 false |
| `EnsureColumn(DataTable, string, Type)` | 파생 컬럼이 없으면 만든다 — 서버가 이미 내려준 컬럼은 그대로 둔다. 프레젠터가 조회 결과에 파생 컬럼(상태·색·활성 플래그)을 보장할 때. table이 null이면 무시 |

### Data 구획 편입 기준 (전부 충족해야 추가한다)

1. 화면/도메인 무관 — 특정 화면 규칙이 조금이라도 섞이면 그 화면의 Presenter로.
2. 무상태 `static`.
3. 입출력이 `DataTable`/기본형뿐 — UI 타입·서버 호출 금지.
4. **2개 이상 화면(프로젝트)에서 실제 사용** — 예측 편입 금지.

회사에 이미 전사 공용 유틸 어셈블리가 있다면 이 구획 대신 그쪽을 쓰고,
`TableHelper` 호출부만 그 유틸로 치환해도 된다 (기능이 단순해 1:1 대응).
