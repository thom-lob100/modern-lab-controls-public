# ModernDataGrid 교체 가이드

- **대체 대상**: `System.Windows.Forms.DataGridView` (목록 조회 화면 + 셀 편집 화면)
- **네임스페이스**: `Modern.Lab.WinForms.Controls.Data`
- 기본은 읽기 전용이다 — 편집 화면은 `ReadOnly = false`를 명시한다 (아래 "셀 편집" 절)

## 데이터 소스와 폐기

명시적 컬럼의 데이터 소스를 교체할 때 AutoFit은 한 번만 수행한다. 배지·행별 버튼 컬럼의 통일 최소폭은
`AutoFitColumns = false`에서도 유지하며, 일반 텍스트 컬럼의 자동 맞춤도 그대로 동작한다.
배지·행별 버튼은 한 번의 폭 계산 안에서 같은 표시 문자열의 실측을 재사용한다. 재사용 범위와 저장량은
제한하며, 한도 이후의 새로운 값도 끝까지 실측한다. 다음 조회·서식·글꼴·DPI 변경에는 새로 계산한다.

폼보다 오래 보관하는 `DataTable`·`DataView`·컬렉션도 사용할 수 있다. 래퍼의 `Dispose`가 외부 데이터
구독과 크기 변경 타이머, 컬럼 감시를 정리하므로 폐기 전에 `DataSource = null`을 따로 넣을 필요는 없다.
`ICollectionView.Filter`에 그리드가 설치한 필터 조건자는 소스 교체·폐기 때 해제하며, 다른 소비자가 교체한 조건자는 보존한다.
외부 뷰가 행 편집·추가 중이면 그 작업을 취소하지 않고 조건자에서 그리드 참조를 끊는다. 뷰의 다음
갱신부터 해당 조건자는 모든 행을 허용한다. 폐기 중 이벤트가 새 소스를 대입해도 다시 구독하지 않는다.
순수 WPF 컨트롤의 임시 `Unloaded`는 최종 폐기로 취급하지 않는다.

`DataTable`·`DataView`의 값 필터는 위 조건자와 달리 `BindingListCollectionView.CustomFilter`를 통해
외부 `RowFilter`(`DataTable`이면 `DefaultView.RowFilter`)를 변경한다. 이 필터 식은 소스 교체·폐기 때
자동으로 해제하거나 이전 식으로 복원하지 않는다. 원본 행은 삭제되지 않지만 같은 뷰를 사용하는 다른
소비자에게도 필터가 보인다. 뷰의 필터를 격리해야 하면 `new DataView(table)`을 그리드 전용으로 만들어
바인딩한다. 공유 뷰의 필터를 초기화할지는 그 뷰를 소유한 폼이 결정한다.

## 호환 제공 멤버

| 멤버 | 비고 |
|---|---|
| `DataSource` | `DataTable`/`DataView`/`IList`/`IEnumerable` 수용. `DataTable`은 내부에서 `DefaultView`로 변환. **컬럼을 선언하지 않았으면 데이터가 컬럼을 정한다** — 아래 "컬럼을 안 적는 경우" |
| `AutoGenerateColumns` | 기본 true. `ConfigureColumns` 호출 시 자동으로 false |

## 컬럼을 안 적는 경우 (2026-08-31)

`ConfigureColumns`를 부르지 않고 `DataSource`만 할당하면 **조회 결과의 컬럼이 그대로 표가 된다.**

```csharp
this.gridHistory.DataSource = this.Request("GetDurableHistory DURABLE_ID=" + id).Table;
// 끝 — 컬럼 목록을 적지 않는다
```

규칙은 `Modern.Lab.Controls.Wpf.Data.AutoColumns` 한 곳에 있고 셋뿐이다.

| 규칙 | 결과 |
|---|---|
| 캡션은 용어사전(`GridCaptionCatalog`) | `LOT_ID` → "Lot Id" |
| 이름이 `_COLOR`로 끝나면 표에 안 내보낸다 | `JUDGE_COLOR`(행 글자색)는 숨는다 |
| 이름이 `_TM`으로 끝나면 시각 | `yyyy-MM-dd HH:mm:ss` + 가운데 정렬. 시각이 아닌 `TIMEKEY`는 접미사가 달라 걸리지 않는다 |

**`ConfigureColumns`를 부르면 그쪽이 이긴다** — 화면이 정한 컬럼을 데이터가 덮지 않는다.
자동으로 나온 컬럼은 조회 결과의 컬럼 구성이 달라질 때만 다시 만들므로, 재조회해도
값 필터·찾기 상태가 풀리지 않는다.

컬럼을 늘리거나 줄이거나 순서를 바꾸는 일은 **쿼리**에서 하고, 화면에는 쿼리 결과만으로는
알 수 없는 것(배지·링크·정렬·문맥 캡션)만 남긴다.

### 그 "알 수 없는 것"을 얹는 자리 — `GridColumns` (2026-08-31)

`Modern.Lab.WinForms.Controls.Data.GridColumns`는 조회 결과로 컬럼을 만든 뒤 화면 사정만
덧붙이는 빌더다. 컬럼 이름을 나열하는 `ConfigureColumns` 블록을 대체한다.

```csharp
GridColumns.Of(ports)
        .Caption("PORT_TYPE", "Type")      // 표 제목이 이미 "Port List"라 여기서는 Type
        .Badge("PORT_TYPE", "MODE", "STATUS")
        .Spin("STATUS", "Run")             // 자기 값이 Run이면 배지가 돈다
        .BadgeSpin("PRIORITY", "RUN_YN")   // 도는 이유가 다른 칸에 있을 때
        .Center("PORT_NO")
        .Bind(this.gridPorts);             // 컬럼 + DataSource 한 번에
```

| 메서드 | 무엇 |
|---|---|
| `Only(...)` / `Hide(...)` | 남길 칸 / 뺄 칸 — 한 조회가 목록과 정보 카드를 함께 채울 때 |
| `Caption(name, text)` | 용어사전과 다르게 부를 때만 |
| `Badge(...)` / `BadgeColor(name, member)` | 값에서 색이 나오는 배지 / 색을 다른 컬럼에서 받는 배지 |
| `Spin(name, values)` / `BadgeSpin(name, activeMember)` | 배지가 도는 조건 — **자기 값**이 그 목록에 있을 때(`BadgeSpinValues`) / **다른 컬럼**이 참인 행일 때(`BadgeSpinBorderMember`). 값 어휘가 열려 있어 값으로 켤 수 없는 배지는 뒤엣것을 쓴다 |
| `BadgeWidth(name, values)` | 배지 폭을 **올 수 있는 값들** 기준으로 고정 — 조회마다 폭이 흔들리지 않는다. 안 쓰면 들어온 데이터의 가장 긴 값이 기본이다 |
| `BadgeWidthByValue(...)` | 통일 폭을 끄고 배지마다 제 값 길이대로 그린다 — 값 길이가 뜻을 갖는 표에만 |
| `Link(...)` / `YesNo(name, enabledMember)` / `SelectBox(name, locked)` | 링크 셀 / 서버가 준 Y/N 체크 / 사람이 고르는 체크 |
| `Center(...)` `Right(...)` `Width(...)` `Format(...)` `Time(...)` | 정렬·폭·표기 |
| `First(...)` | 맨 앞에 둘 칸 — 나머지는 조회 순서 그대로 |
| `Apply(grid)` / `Bind(grid)` | 컬럼만 / 컬럼과 데이터를 한 번에 |

없는 컬럼 이름은 조용히 무시한다 — 쿼리가 바뀌어도 화면이 깨지지 않는다. 같은 컬럼 구성이면
다시 만들지 않으므로 재조회해도 값 필터·찾기 상태가 유지된다.
| `RowCount` | 현재 표시 중인 행 수 (하단 조회 건수 연동용) |
| `SelectedIndex` | 미선택 시 -1. **프로그램으로 설정하면 그 행이 보이도록 자동 스크롤** (DataGridView `CurrentCell` 설정과 같은 체감) |
| `SelectionChanged` | `DataSource` 할당 시 정확히 1회, 이후 사용자 선택 변경 시 발생 |
| `RowDoubleClick` | **행 왼쪽 더블클릭** (2026-08-29 추가) — 그 행이 먼저 현재 행(`SelectedItem`)으로 선택된 뒤 발생하므로 `SelectedItem`을 그대로 쓴다("더블클릭 → 결정 패널" 관례). 헤더·빈 영역에서는 발생하지 않는다 |
| `MultiSelect` | 다중 행 선택 (Shift+클릭 범위 / Ctrl+클릭 개별 토글). **기본 false** — DataGridView 기본(true)과 반대이므로 다중 선택 화면은 `MultiSelect = true`를 명시한다. 켜면 Ctrl+C 행 복사도 선택 행 전부(여러 줄)가 된다 |
| `SelectedItems` | 선택된 행들의 복사본 배열 — **현재 뷰 순서**(정렬/필터 반영), `DataTable` 소스면 각 요소가 `DataRowView`. 기존 `SelectedRows` 순회(`foreach (DataGridViewRow r in grid.SelectedRows)`)는 `foreach (DataRowView row in grid.SelectedItems)`로 이전 |
| `SelectAll()` | 모든 행 선택 (`DataGridView.SelectAll` 대응) — `MultiSelect = true`일 때만 동작 (Ctrl+A/우클릭 메뉴와 동일) |
| `ClearColumnFilters()` / `HasActiveColumnFilters` | 모든 컬럼 값 필터 일괄 해제 / 활성 필터 유무 — 화면 초기화(Reset) 버튼에서 조회 조건과 함께 걷을 때. 우클릭 메뉴 `Clear filters`와 동일 |
| `ModernDataGridColumn.Aggregate` | **집계 푸터** — `GridAggregateKind.Sum`/`Average`/`Count`/`Min`/`Max`. 하나라도 지정하면 그리드 하단(상태바 위)에 컬럼 정렬된 집계 행이 생긴다. 대상은 **현재 뷰**(컬럼 필터 반영)이고 셀 편집 커밋/행 추가·삭제/재조회 시 자동 재계산. 숫자 집계는 숫자로 해석되는 값만(문자열 숫자는 Format 규칙으로 해석), `Count`는 비어 있지 않은 값 수. 표시 서식은 컬럼 `Format`. 페이지 화면에서는 바인딩된 페이지 조각 기준이다 — 전체 합계는 폼이 전체 결과로 계산해 상태바/카드에 표시 |
| `ModernDataGridColumn.HeaderGroup` | **2단 그룹 헤더** — 이웃한 컬럼들이 같은 캡션을 쓰면 그 구간 위에 하나로 합쳐진 그룹 줄이 생긴다 (예: Qty/Purity/Result에 `"Inspection"`). 하나라도 지정되면 그리드 위에 그룹 줄이 추가되고(그룹 없는 컬럼 위는 빈 헤더 배경), 컬럼 폭 조절·자동 맞춤·가로 스크롤·고정 컬럼을 그대로 따라간다. FarPoint Spread의 2단 헤더(셀 병합 헤더) 전환용. **표시 전용** — 정렬/필터/복사/엑셀에는 하위 컬럼 캡션만 쓰인다. 고정(Frozen) 컬럼과 함께 쓸 때 그룹이 고정 경계를 걸치지 않게 구성한다. 한 컬럼 캡션을 두 줄로 쓰는 것은 기존 `"\n"` 다중 줄 헤더가 담당한다 |
| `ModernDataGridColumn.Frozen` | **고정 컬럼** — `DataGridViewColumn.Frozen` 대응. `ConfigureColumns` 정의에 `Frozen = true`를 주면 가로 스크롤 시 그 컬럼까지 왼쪽에 고정된다 (고정은 왼쪽부터 연속 — 중간 컬럼만 켜면 왼쪽 컬럼들까지 함께 고정, DataGridView와 같은 규칙). 그리드 속성 `FrozenColumnCount`(고정 개수 직접 지정)도 있다 |
| `ModernDataGridColumn.MergeCells` | **같은 값 세로 셀 병합** — 정의에 `MergeCells = true`를 주면 **현재 뷰 순서에서 연속으로 같은 값**인 구간이 **하나의 스팬 셀**(단일 표면 + 구간 세로 중앙 값, 내부 행 구분선 없음)로 그려진다 — FarPoint Spread/엑셀의 병합과 같은 시각. 정렬·필터·행 추가/삭제·셀 편집·스크롤을 그대로 따라가며 자동 재계산된다(엑셀처럼 값이 구간 중앙에서 스크롤을 따라 움직인다). **클릭/선택/복사/엑셀 내보내기는 행 단위 그대로**이고(스팬 셀 아래의 실제 행이 반응), 구간의 모든 행을 선택하면 스팬 셀도 선택색으로 칠해진다 — 일부 행만 선택된 동안 중립으로 두는 것은 병합 지원 그리드(DevExpress 등)의 표준 규칙이다(스팬 전체가 칠해지면 "구간 전체 선택"으로 오독된다). 개별 행 hover 하이라이트도 병합 컬럼에서는 보이지 않는다(병합 셀 = 하나의 셀 의미론). **현재 셀 표시**는 병합 셀을 클릭하면 스팬 셀 전체 하단에 액센트 언더라인이 붙는다(엑셀처럼 병합 범위 전체가 현재 셀). 찾기(Ctrl+F)의 노란 강조도 병합 컬럼에서는 스팬 셀에 덮인다(이동은 정상). 병합 컬럼은 정렬된 키/그룹 컬럼에 쓴다 |
| `Enabled` | 전파됨 |

## 계약 보장 동작 (docs/design-notes.md §6-1)

- `DataSource` 할당 시 선택이 초기화되고, 데이터가 있으면 **첫 행이 자동 선택**됨
  (DataGridView의 최초 CurrentRow 동작과 일치) — 이벤트는 1회만 발생
- null/빈 데이터는 빈 그리드로 표시, 예외 없음
- 백그라운드 조회 후 UI 스레드 `Invoke` 할당 패턴 지원
- 컬럼 헤더 클릭 정렬, 컬럼 폭 마우스 조절 지원
- **현재 셀 표시**: 현재 셀(클릭/방향키 이동) 아래에 옅은 액센트 언더라인이
  보인다 (DataGridView의 현재 셀 테두리 대응 — 현재 탭 언더라인과 같은 시각
  언어). 그리드가 포커스를 잃어도 유지되고, 편집 중에는 편집 프레임(2px)이
  대신한다

## 추가 멤버

| 멤버 | 설명 |
|---|---|
| `ConfigureColumns(params ModernDataGridColumn[])` | 명시적 컬럼 정의. `DataSource` 할당 전에 호출. `ModernDataGridColumn(dataPropertyName[, headerText[, width]])` — headerText 생략 시 캡션 용어사전 참조, **사전에도 없으면 DB 컬럼 표기를 읽기 좋게 자동 변환**(`EQP_ID` → `Eqp Id`, 2026-08-06 — 대문자·숫자·언더스코어 표기만 INITCAP 규칙 적용, camelCase 등 다른 표기는 그대로. `GridCaptionCatalog.Humanize`). 앱 시작 시 `Modern.Lab.Hosting.Dictionaries.GridCaptionDictionary.RegisterAll()`(영문) 또는 `Apply(GridCaptionLanguage.Korean)`(한글)을 한 번 호출하면 Common의 회사 표준 캡션을 쓴다. 화면 전용 용어는 `GridCaptionCatalog.Register`/`RegisterRange`로 추가·재정의한다. width 생략/음수는 남은 폭 채움(star). `TextAlignment`(Left/Center/Right) 지정 가능 |
| `ExportXlsx(path, sheetName, data)` | 화면 컬럼 정의 그대로(순서·캡션·`Format`) 데이터 전체를 진짜 .xlsx로 저장 — 외부 라이브러리 없음, 내보내기용 컬럼/헤더 목록을 폼이 따로 관리하지 않는다. CheckBox/Button 컬럼 자동 제외. `data`는 그리드 `DataSource`가 아니라 인자 — 페이지 화면에서도 전체 결과 저장. 저장 대화상자·타임스탬프 파일명·성공/실패 토스트까지는 `ModernFormBase.ExportToExcel(...)` 한 줄로 처리한다(그리드는 `IExcelExportGrid` 계약으로 받는다) |
| `ShowExportMenu` / `ExportSheetName` / `ExportFileNamePrefix` / `ExportXlsxInteractive()` / `ResolveExportData()` | **우클릭 메뉴 "Export to Excel..."** (2026-08-02 추가, 기본 표시 — `ConfigureColumns` 그리드만). 화면의 Excel 버튼(폼이 파일명·토스트까지 다듬는 경로)과 별개로, 그리드 어디서든 우클릭 한 번으로 그 그리드를 저장하는 보조 경로다. 저장 데이터는 `ResolveExportData()` 규칙 — ① `FilterValueSource`(페이지 화면이 깔때기용으로 넣어 둔 **조회 결과 전체**) ② `DataTable` 소스 그대로 ③ `DataView`는 표 변환 ④ `DataRow` 목록(페이지 조각)은 스키마 복제 — 이라 **페이지 화면도 현재 페이지가 아니라 전체가 저장**된다. 시트/파일명은 `ExportSheetName`(빈 값=Data)/`ExportFileNamePrefix`(빈 값=Export, 뒤에 타임스탬프)를 쓴다. 성공은 조용히 끝나고 실패만 메시지 박스 — 화면별 파일명·토스트가 필요하면 기존 `ModernFormBase.ExportToExcel` 버튼 경로를 쓴다. `ShowExportMenu = false`로 항목을 끈다 |
| `ColumnDefinitions` | `ConfigureColumns`로 선언한 정의의 복사본 — 화면과 동일한 컬럼 구성(순서·캡션·형식)으로 커스텀 파생 출력을 만들 때 단일 원천 (엑셀 저장은 `ExportXlsx`가 이미 해준다) |
| `EmptyText` | 데이터 0건일 때 데이터 영역 가운데 표시할 안내 문구 (기본 `"No data"`, 빈 문자열 = 끔). 한국어 화면은 화면에서 지정한다 |
| `CopySelectedRow()` | 현재 행을 **탭 구분 한 줄**로 클립보드에 복사 (`Ctrl+C`와 같은 동작). Excel 붙여넣기 호환 |
| `CopyCurrentCell()` | 현재 **셀 하나**의 표시 텍스트만 복사 (`Ctrl+Shift+C`와 같은 동작). 탭·개행 없음 |
| `ShowCopyMenu` | 우클릭 메뉴에 복사 항목(Copy row / Copy cell)을 넣을지 여부. 기본 `true`. `ContextMenuStrip`이 있으면 **그 메뉴 맨 끝에** 구분선과 함께 붙고, 없으면 복사 항목만 있는 메뉴가 뜬다. **공통 항목의 모양**(2026-08-29): 최상위에는 `Copy row`·`Copy cell`만 오고, `Find...`·`Export to Excel...`·`Select all`·`Clear filters`·`Freeze/Unfreeze columns`는 **`More ▸` 하위 메뉴 하나**에 접힌다(찾기/내보내기 ─ 표 조작 사이 구분선). 하위 항목이 하나도 없으면 `More`도 없다 |
| `ModernDataGridColumn.Format` | 표시 형식 (숫자 `"N0"`, 날짜 `"yyyy-MM-dd HH:mm"` 등). 타입 컬럼은 그대로 적용되고, **문자열 컬럼이라도 값이 날짜/숫자로 해석되면 적용된다** — 서버가 `"2026-07-29 12:00:00.0"`(java Timestamp 문자열)로 내려보내는 날짜 컬럼도 `Format`만 선언하면 화면·복사·찾기·엑셀이 전부 그 형식으로 나온다 (해석 실패 시 원본 그대로, 예외 없음). `DataGridViewCellStyle.Format` 대응. **생성자 4번째 인자로도 선언 가능** (2026-08-12 추가): `new ModernDataGridColumn("SENT_DT", "Sent", 140, "yyyy-MM-dd HH:mm")` — 속성 대입과 완전히 같다 |
| `ModernDataGridColumn.Kind` | 셀 표시 종류 — `Text`(기본) / `CheckBox`(bool 양방향 체크박스, 벌크 대상 지정; OS 기본 룩이 아닌 모던 비주얼 — 둥근 사각 + 액센트 채움 + 흰 체크 글리프, ModernCheckBox와 동일) / `Badge`(`BadgeColorMember` 색 배지 — 모양은 `BadgeShape`로 선택: `Rounded` 둥근 사각 기본 / `Pill` 알약; 같은 컬럼은 가장 긴 표시값 기준 동일 폭) / `Button`(`ButtonText` 캡션 — 행마다 캡션이 달라져야 하면 `ButtonTextMember`로 컬럼을 지정하고, 그 값이 비면 버튼을 그리지 않는다. 행별 캡션 컬럼은 **가장 긴 캡션 기준으로 버튼 폭이 통일**된다(2026-08-13 — 배지 폭 통일과 같은 규칙). `ButtonEnabledMember`로 행별 활성 제어; ModernButton Secondary와 같은 문법 — 평상시 흰 배경 + 회색 테두리, hover 시 옅은 파랑 틴트 + 액센트 테두리/글자. 반복 행 액션의 캡션은 Label 크기 일반 굵기. 캡션 유도색은 `ButtonAutoColor` — 아래 행 참고) / `Combo`(아래 콤보 참고) / `Spinner`(인디터미네이트 회전 원 — 작업 진행중 표시, 아래 참고) / `Link`(액센트색 하이퍼링크 — 클릭 시 `CellLinkClick`, 아래 참고) |
| `ModernDataGridColumn.SpinnerActiveMember` | **Spinner 전용**: 행별로 스피너를 돌릴지 여부로 쓸 컬럼 이름(bool/`"Y"`/`"true"`/`"1"` 참). 참인 행만 **발광 바(액센트 코멧 — 머리가 짙고 꼬리는 투명하게 사라지는 띠가 알약 트랙 위를 좌→우로 흐름 + 글로우)**가 보이고, 나머지는 빈 셀. 값이 아니라 "진행중" 상태만 표시(진행률 수치 없음). 예: 작업 시작(Running) 행에만 스피너 |
| `ModernDataGridColumn.BadgeSpinBorderMember` | **Badge 전용**: 행별로 배지 테두리에 **훑는 하이라이트**(옅은 액센트 베이스 링 + 밝은 코멧 빛띠가 좌→우로 테두리를 지나감, 발광 없음)를 줄지 여부로 쓸 컬럼 이름(bool/`"Y"`/`"true"`/`"1"` 참). 참인 행만 효과가 생긴다(작업중 등 활성 강조). 비우면 없음. 배지가 가로로 납작해 WPF에 원뿔형 그라디언트가 없으므로 둘레를 도는 대신 가로로 훑는다 |
| `ModernDataGridColumn.BadgeSpinValues` | **Badge 전용 — 값 선언형 스핀** (2026-08-07 추가): `"SENDING;PROC"`처럼 배지 **값 목록**을 선언하면 셀 값이 그 중 하나인 행만 훑는 하이라이트가 생긴다 (공백/대소문자 무시). 판정 더미 컬럼이 필요 없다. `BadgeSpinBorderMember`가 지정돼 있으면 그쪽이 우선 |
| `ModernDataGridColumn.BadgeWidthValues` | **Badge 전용 — 폭을 재는 기준 값 목록** (2026-09-07 추가, 세미콜론/쉼표 구분): 비어 있으면 들어온 데이터의 가장 긴 값으로, 값이 있으면 그 목록의 가장 긴 값으로 컬럼 폭을 고정한다. 상태 컬럼처럼 올 수 있는 값이 선언된 자리는 이 목록을 주면 조회 결과와 무관하게 폭이 같다 |
| `ModernDataGridColumn.BadgeWidthByValue` | **Badge 전용 — 통일 폭 끄기** (2026-09-07 추가, 기본 false): 같은 컬럼의 배지는 가장 긴 값에 맞춘 통일 폭으로 그려진다(같은 물건으로 읽히게). `true`면 그 통일을 끄고 값 길이대로 들쑥날쑥 그린다 — 길이 자체가 양을 뜻하는 표에만 쓴다 |
| `ModernDataGridColumn.BadgeAutoColor` | **Badge 전용 — 값 유도 자동색** (2026-08-07 추가): `true`면 색 컬럼 없이 **셀 값 자체**에서 색을 유도한다 (`Palette.FromValue` — 같은 값 = 항상 같은 색, 공백/대소문자 무시). 폼의 색 배열/색 컬럼/프리젠터 색 매핑이 전부 불필요해지는 표준 경로. 색 우선순위: `BadgeColorMember` > `BadgeSemanticMember` > `BadgeGroupMember` > `BadgeAutoColor`. **2026-08-12 변경 — 겹치는 색 자동 회피**: 해시가 같은 30° 색상 슬롯에 겹친 값들(예: Logistics의 "Sent"와 "Request Linked" — 톤 차이뿐인 거의 같은 핑크였다)을 배정 레지스트리가 빈 슬롯으로 벌려 배치한다. 그리드가 바인딩 시점에 컬럼의 고유 값을 정렬 등록하므로 같은 데이터 도메인이면 색이 실행마다 같다. 먼저 만난 36종(색상 12 × 톤 3)까지 상호 구분이 보장되고 그 뒤 값은 예전 규칙(해시+톤 지터)으로 폴백. 이 변경으로 **충돌하던 값들의 색이 바뀔 수 있다** (충돌 없던 값의 색상은 유지, 톤은 지터 제거로 기준 톤에 정리된다) |
| `ModernDataGridColumn.BadgeGroupMember` | **Badge 전용 — 그룹 키 색** (2026-08-07 추가): 쿼리가 CASE WHEN으로 그룹을 판정해 내려주는 표준 경로. 키 컬럼 값이 **0 이상의 정수면 Palette 인덱스 직결**(상호 구분 보장·완전 결정적), 문자열이면 그 키의 `FromValue` 색. 배지 텍스트는 여전히 이 컬럼(`DataPropertyName`) 값이다 |
| `ModernDataGridColumn.BadgeSemanticMember` | **Badge 전용 — 의미 어휘 색** (2026-08-07 추가): 어휘 컬럼(`"Y"`/`"N"`/`"success"`/`"warning"`/`"error"`/`"info"`/`"neutral"`)을 `SemanticColors` 표준색으로 그린다. 모르는 어휘/빈 값은 Neutral (예외 없음). 예: `SEND_YN` 컬럼을 그대로 지정하면 Y=초록/N=빨강 — 색 컬럼도 폼 코드도 불필요 |
| `ModernDataGridColumn.ComboItemsMember` | **Combo 전용**: **행별** 선택지 목록으로 쓸 컬럼 이름. 그 컬럼 셀 값이 `string[]`/`IEnumerable`이면 그 행의 콤보 선택지가 된다 — 행마다 범위가 다를 때(예: STUB 슬롯 1~6 / LCC 슬롯 1~25). 지정하면 컬럼 공통 `ComboItems`보다 우선 |
| `ModernDataGridColumn.HeaderCheckBox` | CheckBox 컬럼 전용 (기본 false). true면 헤더 캡션 대신 **전체 선택/해제 체크박스**가 올라간다 — 클릭 시 현재 그리드에 표시 중인 모든 행(페이지 화면이면 현재 페이지)의 값을 일괄 설정하고, 행 값 상태에 따라 체크(전체)/해제(없음)/중간(일부)으로 자동 갱신. `DataGridView` 헤더 체크박스 커스텀 그리기 코드 대체 |
| `CellButtonClick` | 버튼 컬럼 셀 클릭 이벤트 — `e.Item`(클릭 행 `DataRowView`) + `e.DataPropertyName`(버튼 컬럼 이름). `DataGridView`의 `CellContentClick` + 버튼 컬럼 대체 |
| `ModernDataGridColumn.CheckStyle` | CheckBox 컬럼 전용 (기본 `GridCheckStyle.CheckBox`). `Switch`면 온/오프 스위치로 그린다 — 선택이 아니라 행별 설정 토글용. `DataGridViewCheckBoxColumn`에 스위치 룩을 입히려면 소유자 그리기가 필요했던 자리 |
| `RowKeyMember` / `RestoreSelectionByIndex` | **재조회해도 보던 행이 유지된다** (2026-08-09 추가). `DataGridView`에는 없던 것 — 재바인딩하면 무조건 첫 행으로 돌아가므로 폼마다 "조회 전에 키를 기억했다 조회 후 다시 찾아 선택" 하는 코드를 손으로 짜야 했다. 행 키 컬럼만 선언하면 그리드가 대신 한다(스크롤 위치까지). 스크롤 위치도 함께 되돌리되, 그 행이 멀리 이동해 **선택이 화면 밖이 되면 선택 쪽으로 맞춘다** (어디를 보고 있었는지보다 무엇이 선택돼 있는지가 중요하다). 복합 키는 콤마로 나열. 키가 사라졌을 때 같은 행 번호를 지키려면 `RestoreSelectionByIndex = true` |
| `RowColorSelector` | `Func<object, string>`으로 행 배경색을 계산. DataTable/DataView의 행 객체는 DataRowView. 소스에 보조 컬럼을 만들지 않는다. 선택·hover > selector > RowColorMember > 기본 배경 순서. 빈 값·해석 불가 색·계산 예외는 다음 순위로 폴백하며 외부 상태 변경 후 selector를 새 함수로 지정해 갱신한다 |
| `RowMarkerSelector` | 행 객체에서 짧은 모서리 표식 문자열을 계산. 첫 번째 보이는 열의 왼쪽 위에 8 DIP로 표시하며 행 높이·열 너비·클릭 영역을 바꾸지 않는다. 빈 값·예외는 숨김. 테마의 WarningText(주황 계열) 색을 쓴다 — 선택 행의 파란 배경·글자와 구분된다(2026-09-26, 전에는 SuccessText). 처음 나타날 때 1.4초 동안만 밝기를 변화시킨 뒤 고정한다. Windows 애니메이션 끄기를 존중한다. 외부 상태 변경 후 새 함수로 지정해 갱신하며 데이터 소스 또는 함수 교체 시 최초 강조 기준을 초기화한다 |
| `SelectRow(predicate)` | `Func<object, bool>` 조건에 맞는 현재 정렬·필터 뷰의 첫 행을 선택하고 스크롤한다. 성공 true. 일치 행 없음/null 조건은 false와 기존 선택 유지. 조회·쓰기 요청을 만들지 않는다 |
| `RowForegroundMember` | **행 전체 글자색** (2026-08-29 추가). 값이 `"#DC2626"` 같은 색 문자열인 컬럼을 지정하면 그 행의 텍스트 셀 글자가 그 색이 된다 — 판정 FAIL 행을 빨간 글자로. `DataGridView`의 `RowPrePaint`/`CellFormatting`에서 `DefaultCellStyle.ForeColor`를 행마다 바꾸던 코드 대체. 배경은 `RowColorMember`(같은 문법). 배지/링크/버튼 셀은 자기 색 유지, 선택 셀 글자색 우선 |
| `ModernDataGridColumn.BadgeAccentValues` | **특정 값만 강조** (2026-08-09 추가). 값 목록을 세미콜론/쉼표로 적으면 그 값의 배지만 진한 오류 채움 + 테두리가 된다 — 나머지는 원래 색 규칙 그대로. `DataGridView`에서 `CellFormatting`으로 행마다 색을 칠하던 코드를 선언 한 줄로 대체한다. 채움과 테두리를 함께 쓰는 것은 대비 때문이다 — 글자만 빨갛게 하면 테마 대부분에서 안 읽히고, 색만 바꾸면 HighContrast·CrimsonGray에서 파스텔 칩과 겹친다 |
| `ModernDataGridColumn.CheckEnabledMember` | CheckBox 컬럼 전용: 행별 토글 가능 여부 컬럼(bool/`"Y"`/`"true"`/`"1"`). 거짓인 행은 잠긴 모양 + 클릭 무시. 잠긴 채 **체크된** 행은 회색 채움 + 흰 체크로 "선택돼 있으나 바꿀 수 없음"이 보인다(2026-08-29 — 그 전엔 흰 글리프가 비활성 배경에 묻혀 체크 안 된 것처럼 보였다). `DataGridView`에서 셀 `ReadOnly`를 행마다 세팅하던 코드 대체 |
| `ModernDataGridColumn.ButtonAutoColor` | **Button 전용 — 캡션 유도 자동색** (2026-08-12 추가): `true`면 버튼 색을 **캡션 값 자체**에서 유도한다 — 배지 `BadgeAutoColor`와 같은 `Palette.FromValue` 규칙(같은 캡션 = 항상 같은 색·겹치는 색 자동 회피, 공백/대소문자 무시). **배경은 중립 Secondary 버튼과 같은 표면 그대로**(2026-08-13 변경 — 칩 채움은 너무 강렬했고 틴트는 흰색도 색도 아닌 애매한 면이었다)이고, 색 구분은 칩 색 계열의 **글자·테두리**가 담당한다(대비 4.5:1 보장). **hover에서 그 값의 배지 칩(파스텔)이 채워지고** pressed는 한 단계 짙어진다 — hover 색이 같은 값의 배지와 정확히 같다. `ButtonTextMember`(행별 캡션 — 예: Accept/Receive/Create)와 조합하면 행마다 색이 갈린다. 캡션이 빈 행/해석 불가 값은 중립 버튼 색, 비활성 행은 기존 회색 처리 그대로. `DataGridView`에서 `CellFormatting`으로 버튼 셀 배경을 행마다 칠하던 코드를 선언 한 줄로 대체한다 |
| `ModernDataGridColumn.CheckTrueValue` / `CheckFalseValue` | **CheckBox 전용 — 값 어휘** (2026-08-11 추가). `DataGridViewCheckBoxColumn.TrueValue`/`FalseValue`와 같은 자리다. 값 컬럼이 bool이 아니라 `"Y"`/`"N"` 같은 **문자열**일 때 그 어휘를 선언하면 **조회가 이미 내려주는 컬럼에 체크 상태를 그대로 물릴 수 있다** — 화면용 bool 컬럼을 따로 만들지 않는다. 표시는 어휘 또는 일반 참 규칙(`"Y"`/`"YES"`/`"TRUE"`/`"1"`)으로 읽고, 토글·헤더 전체 선택은 **원본 어휘 그대로** 되쓴다(체크 → `CheckTrueValue`). 널/빈 값/모르는 값은 해제(예외 없음). **둘 다 채워야** 켜진다 — 한쪽만 채우면 끈 자리에 쓸 값이 정해지지 않으므로 bool 컬럼으로 취급한다. 비워 두면 지금까지와 같은 bool 컬럼 |
| `CellCheckChanged` | 체크박스 컬럼 값 변경 이벤트 — `e.Item` + `e.DataPropertyName` + `e.IsChecked`. `DataGridView`에서 `CellValueChanged` + `CurrentCellDirtyStateChanged`(체크박스는 커밋을 강제해야 했다) 조합으로 만들던 즉시 반영 처리를 대체한다. 헤더 체크박스 일괄 변경은 값이 바뀐 행마다 발생 |
| 행 우클릭 + `ContextMenuStrip` | 행 위에서 우클릭하면 표준 규칙(선택 안 된 행 → 선택, 이미 선택된 행 → 선택 보존)으로 선택이 처리되고 **클릭한 셀이 현재 셀로** 갱신된 뒤, 컨트롤에 지정한 `ContextMenuStrip`이 커서 위치에 뜬다 — 메뉴 핸들러는 `SelectedItem`(다중이면 `SelectedItems`)을 대상으로 처리하면 된다. 행 밖(헤더/빈 영역) 우클릭에는 뜨지 않는다. 그리드 공통 항목(복사/찾기/엑셀 내보내기/전체 선택/필터 해제/컬럼 고정)은 그 메뉴 맨 끝에 자동으로 붙는다(아래 참고) |
| (스크롤 동작) | 세로 스크롤은 **픽셀 단위**다(`VirtualizingPanel.ScrollUnit=Pixel`, 행 가상화 유지). 기본(줄 단위)에서는 몇 px만 잘린 행 — 눈에는 온전해 보인다 — 을 클릭하면 "선택 행은 완전히 보이게" 규칙이 한 줄을 통째로 점프시켜 어색했다. 픽셀 단위면 모자란 픽셀만큼만 움직인다. 스크롤 위치에 따라 위/아래 행이 반쯤 걸칠 수 있다(모던 그리드 공통 문법) |
| `AllowFindPanel` / `ShowFindPanel()` / `HideFindPanel()` / `IsFindPanelOpen` | **Ctrl+F 찾기** (기본 true) — 엑셀의 찾기 대화상자 방식. 행을 거르는 필터가 아니라 **일치 위치로 이동**한다. Ctrl+F(또는 우클릭 메뉴 `Find...`, `ShowFindPanel()`)로 그리드 오른쪽 위에 **소유된 모덜리스 찾기 창**이 열린다 — 표준 찾기 대화상자처럼 호스트 창 위에만 항상 표시(다른 앱 위로는 안 감), 호스트 최소화에 동행, 작업표시줄/Alt-Tab 미표시, **제목줄 드래그로 이동**(옮긴 위치는 세션 동안 유지), 열린 채 Ctrl+F를 다시 누르면 검색창 재포커스, **그리드가 화면에서 숨겨지면(셸/탭/MDI에서 다른 메뉴로 전환, 패널 숨김) 찾기 창도 함께 내려간다**(2026-08-06 — 소유자인 최상위 창은 화면 전환에도 그대로 활성이라 스스로는 안 사라지므로 그리드 가시성에 연동; 2026-08-07 — 그리드 자신이 아니라 **조상 컨테이너만 숨는 경우**도 래퍼가 조상 체인 가시성을 구독해 내린다 — WinForms는 조상 숨김을 자손 VisibleChanged로 전파하지 않는다; 2026-08-25 — 화면을 숨기지 않고 **다른 화면을 앞으로 가져와 덮는 전환**(셸 홈 버튼 등 z-순서 교체)도 내린다 — 이 경우 가시성 이벤트가 전혀 없어 조상 Layout에서 완전 가림을 실측해 연동한다). **Find Next**(검색창 Enter/F3)는 다음 일치 셀로 선택·스크롤 이동(끝나면 처음부터 순환), **Shift+F3/Shift+Enter**는 이전 일치, **Find All**은 일치 목록(`Row n · 컬럼 · 값`)을 창 안에 나열하며 항목을 클릭하면 그 셀로 이동한다. 화면에 보이는 일치 텍스트는 **노란 배경으로 강조**된다(`Brush.WarningBorder` 토큰 — 가상화로 새로 나타나는 행에도 자동 적용). 대상은 텍스트를 담은 컬럼(Text/Link/Badge; 자동 생성 그리드는 모든 바인딩 컬럼), 화면 표시 텍스트 기준(`Format` 반영) 부분 일치·대소문자 무시, 검색 순서는 현재 뷰의 행 순서(정렬·컬럼 필터 반영). Esc/✕로 닫으면 강조가 걷힌다. 행을 거르지 않으므로 페이징 화면 연동 코드가 필요 없다 — 단 검색 범위는 그리드에 바인딩된 것(현재 페이지)뿐이다. `IsFindPanelOpen`은 찾기 창 열림 상태(읽기 전용 — 자체 검사·토글 UI용). **Ctrl+F 키 자체는 그리드에 포커스가 있을 때 처리된다** — 조회 조건·버튼에 포커스가 있어도 열리게 하려면 폼 `ProcessCmdKey`에서 Ctrl+F를 받아 `ShowFindPanel()`을 부른다 (Samples의 `ModernFormBase.RegisterFindShortcut` 참고) |
| `ModernDataGridColumn.Kind = Link` + `CellLinkClick` | **하이퍼링크 컬럼** — 값 텍스트가 액센트색 링크(**항상 밑줄**, 호버 시 진한 액센트)로 그려지고, 클릭하면 `CellLinkClick`이 발생한다(`e.Item` = 클릭 행 `DataRowView`, `e.DataPropertyName` = 링크 컬럼 이름; 클릭한 행이 먼저 현재 행으로 선택된다). "Lot ID 클릭 → 이력 화면" 관례용 — `DataGridView`에서 `DataGridViewLinkColumn` + `CellContentClick`으로 만들던 자리다. 정렬/값 필터/복사/자동 폭은 텍스트 컬럼과 동일 |
| `AllowColumnFilters` | 컬럼 헤더 깔때기 값 필터(엑셀식 고유 값 체크리스트, **기본 true**). 여러 값을 체크한 뒤 **Apply로 확정**한다(`Clear` = 필터 즉시 해제, ✕/바깥 클릭 = 변경 취소). 팝업 검색창은 한글 초성 매칭으로 체크리스트 자체를 좁히고(제안 드롭다운 없음), **검색에 맞는 값만 체크된 상태**가 되어 그대로 Apply 하면 그 값들로 걸러진다(검색어를 지우면 다시 전체 선택). 화면 뷰만 거르고 원본 표의 행은 지우지 않는다(단 `DataTable`/`DataView` 소스에서는 그 뷰가 **공유 `DefaultView`**라 다른 소비자에게도 남는다 — 위 "데이터 소스와 폐기" 경계) — `DataGridView`에서 직접 구현하던 헤더 필터 커스텀 코드 대체. 재조회(`DataSource` 재할당) 후에도 필터 선택 유지. 깔때기는 헤더 셀 맨 오른쪽 고정, 정렬 글리프가 그 왼쪽 |
| `FilterValueSource` / `ColumnFiltersChanged` / `MatchesColumnFilters(row)` | **부분집합을 바인딩하는 화면의 필터 연동 3종** — 조회 결과 전체가 아니라 그 일부(필터 통과 행만, 또는 페이지 조각)를 바인딩하는 화면은 ① `FilterValueSource`에 전체 결과 `DataTable`을 지정해 깔때기 체크리스트에 전체 값이 나오게 하고, ② `ColumnFiltersChanged` 이벤트에서 전체 결과를 `MatchesColumnFilters(row)`로 걸러 표시 행 목록을 다시 만든다 (Samples의 `LogisticsRequestForm` 참고 — 페이징은 없지만 필터 결과 복사본을 바인딩한다). 조회 결과를 그대로 바인딩하는 화면은 셋 다 필요 없다 |
| `SelectedItem` | 선택 행 (`DataTable` 소스일 때 `DataRowView`) — 기존 `CurrentRow.DataBoundItem` 대체. 프로그램으로 설정하면 그 행이 보이도록 자동 스크롤 |
| `EnableColumnVirtualization` | **기본 false.** WPF DataGrid의 같은 이름 속성을 그대로 노출한다. 켜면 가로 뷰포트 밖 컬럼의 셀을 만들지 않아 컬럼이 많은 표의 첫 표시와 세로 스크롤이 빨라진다(실측 64컬럼·2000행, `ConfigureColumns` + `AutoFitColumns`: 첫 표시 1279→479ms, 세로 스크롤 496→162ms). 대신 가로로 크게 옮길 때 새 컬럼의 셀을 그때 만들어 200ms 안팎이 든다. **컬럼이 가로로 넘치고 폭이 절대값일 때 효과가 있다** — `AutoFitColumns`를 켠 표(자동 생성 컬럼 포함, 측정 뒤 고정 픽셀 폭)가 그 조건이다. 폭 지정도 `AutoFitColumns`도 없으면 컬럼 정의의 기본 폭이 `*`라 컬럼이 화면에 다 들어가 셀 수가 같고, 켜도 차이가 없다. 세로 목록 위주의 넓은 읽기 전용 표에서 화면 단위로 켜고, 켠 화면은 셀 병합·고정 컬럼 표시를 확인한다. 참고: WPF는 가상화 중에도 고정 컬럼 옆 첫 일반 컬럼과 마지막 컬럼을 폭 계산용으로 늘 실체화해 둔다(컨트롤 계약 검사가 이 특성을 전제로 뷰포트 밖 가운데 컬럼으로 판정한다) |
| `AutoFitColumns` | true면 각 컬럼 너비를 **헤더 캡션과 데이터 내용 중 더 넓은 쪽**에 맞춰 자동 계산 (`ConfigureColumns` 컬럼에만 적용, 컬럼 정의의 `Width`는 무시 — 단 CheckBox/Button/Badge 컬럼은 정의 폭·캡션 기준). `DataSource`가 바뀔 때마다 재계산되며 하한 48px / 상한 600px. 사용자의 마우스 폭 조절은 그대로 가능. 헤더 폭에는 캡션 외에 **값 필터 깔때기**(`AllowColumnFilters`가 켜진 컬럼만) 몫이 예약된다 — 폭은 상수가 아니라 아이콘 폰트/크기 토큰으로 실측한다. **정렬 글리프(▲/▼) 몫은 정렬이 걸린 컬럼에만** 예약하므로, 헤더를 클릭해 정렬하면 그 컬럼이 글리프 폭(약 10px)만큼 넓어지고 오른쪽 컬럼들이 한 번 밀린다 — 평상시 컬럼을 좁게 유지하기 위한 선택이다 |

정렬 글리프(▲/▼)는 헤더 텍스트 옆이 아니라 **헤더 셀 오른쪽 끝에 고정** 표시된다 —
컬럼 너비와 무관하게 위치가 일정해 정렬 상태를 훑어보기 쉽다.

## 셀 편집 (`ReadOnly = false`)

**기본은 읽기 전용(`ReadOnly = true`)이다** — `DataGridView`(기본 편집 가능)와
반대다. 기존에 변환된 조회 화면이 코드 변경 없이 그대로 동작하게 하기 위한
선택이므로, 편집 화면을 변환할 때는 `.Designer.cs` 또는 폼 코드에
`grid.ReadOnly = false;`를 반드시 명시한다.

| 멤버 | 설명 |
|---|---|
| `ReadOnly` | 기본 `true`(잠김). `false`면 **텍스트 컬럼**을 더블클릭/F2/타이핑으로 편집한다. Enter = 확정 후 아래 셀, Tab = 오른쪽 셀, Esc = 취소 (`DataGridView`와 동일) |
| `ModernDataGridColumn.ReadOnly` | 컬럼 단위 잠금 (기본 false). 키 컬럼(ID 등)처럼 값이 바뀌면 안 되는 컬럼에 `true` — `DataGridViewColumn.ReadOnly` 대응 |
| `CellValueChanged` | 편집이 커밋되어 **값이 실제로 바뀌었을 때** 발생 — `e.Item`(행 `DataRowView`) + `e.DataPropertyName` + `e.OldValue`/`e.NewValue`(원본 컬럼 타입으로 변환된 값). 같은 값 확정/Esc 취소에는 발생하지 않는다 |
| `CellValidating` | 커밋 직전 발생 — `e.NewText`(편집기 표시 문자열)를 보고 `e.Cancel = true`면 커밋이 취소되고 셀이 편집 상태로 남는다 (`DataGridView.CellValidating` 대응). **타입 오류는 이 이벤트 없이 그리드가 스스로 막으므로**(아래) 값 범위·중복 같은 업무 규칙만 검증한다 |
| `EndEdit()` | 열려 있는 셀/행 편집을 확정 — **저장 버튼 처리 첫 줄에서 호출**해야 편집 중이던 값이 `DataTable`에 반영된 뒤 `GetChanges()`를 읽을 수 있다 (`DataGridView.EndEdit` 대응) |
| `CancelEdit()` | 열려 있는 편집을 버린다 (Esc와 동일) |
| `AddRow()` / `AddRow(params object[] values)` | 새 행 추가 + 선택/스크롤 — `DataGridView.Rows.Add` 대응. `values`는 `ConfigureColumns` 선언 순서의 **데이터 컬럼**(Button/Spinner 상태 컬럼 제외)에 차례로 들어간다. 반환값은 새 행(`DataRowView`) — `row["COL"] = value`로 나머지를 채울 수 있다. `DataTable`/`DataView`/`IBindingList` 소스 전용 |
| `RemoveSelectedRow()` / `RemoveRow(item)` | 행 제거 + 이웃 행 선택 — `DataGridView.Rows.Remove` 대응. `DataTable` 소스는 `Delete` 표시가 되어 저장 로직(`GetChanges`)이 삭제를 그대로 본다 |

동작 규칙:

- **숫자/날짜 컬럼은 표시 서식 그대로 편집된다** — `Format = "N0"` 컬럼은
  편집기에 `1,234`가 보이고 콤마째 고쳐도 숫자로 되돌려 커밋한다.
  `"yyyy-MM-dd"` 날짜 컬럼은 `2026-07-31` 문자열로 편집한다.
- **타입에 안 맞는 입력은 커밋이 거부된다** — 숫자 컬럼에 문자를 넣으면
  편집기가 빨간 테두리로 남고 값은 원본에 들어가지 않는다. 폼이 따로 막을
  필요가 없다.
- 셀 커밋 즉시 행 편집까지 확정되어 `DataTable`에 반영된다(RowState 변경) —
  "편집 → 저장 버튼에서 `EndEdit()` + `GetChanges()` 전송 → `AcceptChanges()`"
  패턴이 그대로 동작한다.
- **편집 중 표시**: 행 하이라이트(선택색)는 편집 중에도 그대로 유지되고,
  편집 중인 셀만 흰 표면 + 액센트색 2px 테두리 프레임(엑셀의 편집 셀 문법)으로
  구분된다. 폼이 설정할 것 없음.
- 템플릿 컬럼(CheckBox/Combo/Badge/Button/Spinner/Link)은 `ReadOnly`와 무관하다 —
  체크박스/콤보는 지금처럼 항상 양방향이고, 나머지는 항상 잠김이다.
- **페이지 조각/필터 결과 복사본을 바인딩하는 화면 주의**: 편집은 그리드에
  바인딩된 것(복사본)에 쓰인다 — 체크박스 컬럼과 같은 규칙으로 원본 동기화
  (`ColumnChanged` 구독)가 필요하다. 전체 결과를 그대로 바인딩하는 편집
  화면에는 해당 없음.

## 클립보드 복사

| 조작 | 결과 |
|---|---|
| `Ctrl+C` | **선택 행** 전체 — 탭 구분 (Excel에 붙이면 열로 나뉜다). 다중 선택이면 선택된 행 전부가 **뷰 순서대로 여러 줄**, 아니면 현재 행 한 줄 |
| `Ctrl+Shift+C` | **현재 셀** 하나 — 탭·개행 없는 순수 텍스트 |
| 우클릭 | 탐색기/엑셀 표준 규칙 — 선택 안 된 행 위 우클릭은 그 행을 선택하고, **이미 선택된 행(다중 선택 포함) 위 우클릭은 선택을 보존**한다 (`Copy row`가 선택 전체를 복사할 수 있게). 클릭한 셀은 항상 현재 셀로 갱신(언더라인 이동)되어 `Copy cell`과 컬럼 고정의 기준이 된다. 기본 항목 `Copy row` / `Copy cell` / `Find...` + 상태에 따라 `Select all`(다중 선택 그리드, Ctrl+A) / `Clear filters`(값 필터가 걸려 있을 때) / `Freeze columns up to here`(그 셀의 컬럼까지 왼쪽 고정 — 엑셀 틀 고정과 달리 **컬럼만**) / `Unfreeze columns`(고정이 있을 때) |

- **데이터가 아닌 컬럼은 제외된다** — 체크박스·버튼·스피너 컬럼은 상태 표시라
  복사에 끼지 않는다. 예: 체크박스 + LOT ID + 수량 + 상태배지 + 버튼 구성에서
  행 복사 결과는 `IT10001ED⇥25⇥진행`이다. WPF 기본 Copy는 이 컬럼들을 빈 칸으로
  넣어 붙여넣은 표의 열이 밀리므로 쓰지 않는다.
- 셀 값의 서식은 화면과 같다(`ModernDataGridColumn.Format` 적용).
- 선택 단위는 **행 그대로**다(`SelectionUnit=FullRow`). 셀 단위 선택으로 바꾸면
  WPF가 `SelectedItem`을 유지하지 않아 `SelectedItem`/`SelectionChanged`에 의존하는
  폼 코드가 깨지므로 바꾸지 않았다.
- **폼이 자체 `ContextMenuStrip`을 써도 공통 항목은 나온다** — 메뉴를 띄우기 직전에
  그 메뉴 맨 끝에 구분선 + `Copy row` / `Copy cell` + `More ▸`(찾기·내보내기·표 조작)가
  붙는다. 폼마다 항목을 넣어 줄 필요가 없다. 작은 표에서 `More`까지 필요 없으면
  `AllowFindPanel = false`·`ShowExportMenu = false`로 끈다(샘플 Equipment Port의 Port List).
  - 여러 번 우클릭해도 항목이 늘어나지 않는다(열기 직전에 떼고 다시 붙인다).
  - 하나의 메뉴를 여러 그리드가 공유해도 **우클릭한 그리드**가 복사 대상이 된다.
  - 그리드를 `Dispose`하거나 `ContextMenuStrip`을 런타임에 교체하면 주입했던
    공통 항목은 자동으로 걷힌다 — 화면을 동적으로 교체해도 폐기된 그리드가
    폼 메뉴에 남지 않는다.
  - 폼이 런타임에 자기 항목을 넣고 빼도 복사 항목은 항상 맨 끝에 온다.
  - 메뉴 문구를 화면 표기 언어에 맞추거나 위치를 직접 정하려면 `ShowCopyMenu = false`로
    끄고 `CopySelectedRow()` / `CopyCurrentCell()`을 호출하는 항목을 직접 넣는다.
- 클립보드를 다른 프로세스가 잡고 있으면 복사는 조용히 실패한다 — 화면은 죽지 않는다.

## 정렬

컬럼 헤더 클릭으로 정렬하고, **Shift+클릭으로 다중 정렬**(2차·3차 기준 추가)한다 —
WPF DataGrid 기본 동작이다. 체크박스·버튼 컬럼은 정렬 대상이 아니다.

**페이징 화면에서는 현재 페이지만 정렬된다.** 그리드는 자기에게 바인딩된 것만
정렬하므로, 페이지 조각(예: 50행)을 넣는 화면에서는 그 조각 안에서만 정렬된다 —
그냥 클릭이든 Shift+클릭이든 마찬가지다(둘의 차이는 정렬 기준 개수뿐, 범위가 아니다).
전체 결과를 정렬해야 하면 폼이 전체 결과를 정렬한 뒤 페이지를 다시 잘라 넣는다
(컬럼 필터가 `ColumnFiltersChanged`로 같은 문제를 푸는 방식과 동일한 구조).

## 미지원 멤버와 대체 방법

| 기존 멤버 | 대체 |
|---|---|
| `CurrentRow`, `SelectedRows`, `Rows[i].Cells[...]` | `SelectedItem`(`DataRowView`)으로 값 접근: `((DataRowView)grid.SelectedItem)["EMP_NO"]` |
| `Columns` 컬렉션 (디자이너 정의 포함) | `ConfigureColumns(...)` 코드 정의 |
| 셀 편집 (`ReadOnly = false`, `CellValueChanged`) | **지원** — 단 기본값이 `DataGridView`와 반대(`true` = 잠김)이므로 편집 화면은 `ReadOnly = false`를 명시한다. 위 "셀 편집" 절 참고 |
| `DataGridViewCheckBoxColumn` | `ConfigureColumns`에 `Kind = GridColumnKind.CheckBox` 컬럼 (bool 컬럼 바인딩) |
| `DataGridViewButtonColumn` + `CellContentClick` | `Kind = GridColumnKind.Button` 컬럼 + `CellButtonClick` 이벤트 |
| `DataGridViewLinkColumn` + `CellContentClick` | `Kind = GridColumnKind.Link` 컬럼 + `CellLinkClick` 이벤트 |
| `DataGridViewComboBoxColumn` | `Kind = GridColumnKind.Combo` 컬럼 — `ComboItems`(고정 선택지 `string[]`, 예: `{"SUCC","FAIL"}`) 중 하나를 고르면 원본 행 컬럼 값이 즉시 갱신된다. **행마다 선택지가 다르면** `ComboItemsMember`에 `string[]`을 담은 컬럼 이름을 지정한다(예: STUB행 1~6 / LCC행 1~25). `ComboEnabledMember` 컬럼 값(bool/`"Y"`/`"true"`/`"1"`)으로 행별 입력 가능 여부 제어 (비활성 행은 회색 잠금). `ComboItemColors`(선택지와 같은 순서의 색 배열)를 주면 선택 값/드롭다운 항목이 **레티클(둥근 사각) 배지**로 표시되고 필드 표면 전체도 선택 값의 배지 색으로 칠해진다 (미선택은 기본 필드). 입력분만 서버로 보내려면 바인딩 직전 `table.AcceptChanges()` 후 `table.GetChanges()`를 전송. 판정/등급 입력용 |
| 셀에 로딩/진행중 표시 | `Kind = GridColumnKind.Spinner` 컬럼 + `SpinnerActiveMember`(bool 컬럼). 참인 행만 인디터미네이트 회전 원이 돈다 — "작업 시작 시 그 행이 처리중"을 셀 안에서 보여 줄 때. 값 표시가 아니라 상태 표시라 정렬 대상이 아니다 |
| `CellClick`/`CellDoubleClick` | `SelectionChanged`로 선택 처리. 더블클릭 이벤트가 필요하면 별도 요청 |
| `MultiSelect` | **지원** — 단 기본값이 `DataGridView`와 반대(false = 단일 선택)이므로 다중 선택 화면은 `MultiSelect = true`를 명시한다. 결과는 `SelectedItems`(뷰 순서 `DataRowView[]`). 대량·비연속 대상 지정은 체크박스 컬럼(`Kind = CheckBox` + `HeaderCheckBox`)이 여전히 더 맞을 수 있다 |
| `DefaultCellStyle`, `Font`, 색상 계열 | 없음 — 토큰이 결정 |

## .Designer.cs 교체 예시

```csharp
// 변경 전
private System.Windows.Forms.DataGridView gridEmployee;
this.gridEmployee = new System.Windows.Forms.DataGridView();

// 변경 후
private Modern.Lab.WinForms.Controls.Data.ModernDataGrid gridEmployee;
this.gridEmployee = new Modern.Lab.WinForms.Controls.Data.ModernDataGrid();
```

폼 코드에서 컬럼 정의 후 서버 응답을 그대로 할당한다:

```csharp
using Modern.Lab.Controls.Wpf.Data; // ModernDataGridColumn

this.gridEmployee.ConfigureColumns(
    new ModernDataGridColumn("EMP_NO", "사번", 90),
    new ModernDataGridColumn("EMP_NAME", "이름", 110),
    new ModernDataGridColumn("HIRE_DATE", "입사일") { TextAlignment = GridTextAlignment.Center });

this.gridEmployee.DataSource = replyTable;   // 서버 응답 DataTable
this.lblCount.Text = "조회 " + this.gridEmployee.RowCount + "건";
```

권장 크기: 영역을 채우는 컨트롤이므로 `Dock = Fill` 또는 `Anchor` 4방향 지정.

## 여러 개를 한 폼에 놓을 때 — 첫 표시 시간 (2026-09-02)

이 컨트롤은 WPF 섬(ElementHost)이다. 섬은 하나마다 GPU 렌더 타깃을 만드는데, 그 생성이 PC·그래픽
드라이버에 따라 **섬당 ~100ms**까지 든다(빈 `Border` 하나짜리 순정 ElementHost도 같다). 표 5개·KPI 카드 4장·
콤보 2개인 화면은 뜨는 데만 3초가 넘었다. 앱 시작 시(첫 폼 전) 한 줄로 소프트웨어 렌더링으로 고정하면
섬당 ~7~10ms다 — 업무 화면은 그림이 같고, RDP는 원래 소프트웨어로 그린다:

```csharp
Modern.Lab.WinForms.Controls.Hosting.WpfHostOptions.SoftwareRendering = true;
```

기본값은 off(켜지 않으면 기존과 같다). 자세한 것은 `controls-reference.md`의 "WPF 소프트웨어 렌더링".
