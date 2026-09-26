# ModernComboBox 교체 가이드

- **대체 대상**: `System.Windows.Forms.ComboBox`
- **네임스페이스**: `Modern.Lab.WinForms.Controls.Selection`

## 호환 제공 멤버

| 멤버 | 비고 |
|---|---|
| `DataSource` | `DataTable`/`DataView`/`IList`/`IEnumerable` 수용. `DataTable`은 내부에서 `DefaultView`로 변환 |
| `DisplayMember` | 표시 텍스트 컬럼/속성 이름 |
| `ValueMember` | `SelectedValue` 컬럼/속성 이름 |
| `SelectedValue` | **순서에 관대하다** — ① `DataSource`보다 먼저 설정해도 값을 보류했다가 데이터 도착 시 적용, ② `DataSource`를 먼저 할당하고 `ValueMember`를 나중에 줘도 값이 나온다(읽는 시점에 현재 `ValueMember`로 해석), ③ **수동 `Items` 컬렉션에서도 동작** — `Items.Add` 뒤에 설정하면 즉시 선택되고, 먼저 설정하면 일치 항목이 `Items.Add`로 나타나는 시점에 적용된다. `ValueMember`가 비어 있으면 원본처럼 항목 자체(`DataRowView`)를 돌려준다 |
| `SelectedItem` | `DataTable` 소스일 때는 `DataRowView` 반환 (기존 WinForms 바인딩과 동일) |
| `SelectedIndex` | 미선택 시 -1 |
| `Items` | 수동 항목 컬렉션 (`Items.Add(...)`). `DataSource` 지정 시 `DataSource`가 우선 |
| `SelectedIndexChanged` | `DataSource` 할당 시 정확히 1회, 이후 사용자 선택 변경 시 발생. **사용자가 드롭다운에서 이미 선택된 항목을 다시 골라도 발생**(WinForms `CBN_SELCHANGE` 호환, 2026-08-26) — `DataSource`가 자동 선택한 첫 항목을 사용자가 그대로 고르는 경우가 이 경로다 |
| `DropDown` | 드롭다운 목록이 **열리기 직전** 발생 (WinForms `ComboBox.DropDown` 호환, 2026-08-26). 열 때 목록 지연 로딩 등에 쓴다. 검색형 콤보는 타이핑으로 목록이 자동으로 열릴 때도 발생한다(원본에 없던 자동 개폐지만 사용자 개폐와 같은 계약) |
| `DropDownClosed` | 드롭다운 목록이 **닫힌 직후** 발생 (WinForms `ComboBox.DropDownClosed` 호환, 2026-08-26) |
| `DropDownStyle` | **타이핑 허용 여부**만 정한다(원본과 같은 의미). `DropDown`(기본, WinForms 기본값과 동일) = 입력 가능, `DropDownList` = 선택 전용. **입력이 목록을 좁힐지는 `AutoCompleteMode`가 정한다**. 마우스로 여는 방식도 원본과 같다 — `DropDown`은 **셰브런(화살표)을 눌러야** 목록이 열리고 입력 칸과 그 둘레(테두리·여백)를 누르면 캐럿만 놓인다. `DropDownList`는 필드 어디를 눌러도 열린다(2026-09-26) |
| `AutoCompleteMode` | 기본 `None`(자유 입력). **네 값이 각각 다르게 동작한다** — `Suggest`: 입력한 글자로 목록을 좁히고 드롭다운을 연다(한글 초성 검색 포함, "ㄱ" → 개발1팀·개발2팀·경영지원팀). `Append`: 목록은 그대로 두고 입력 뒤에 **남은 글자를 옅게 겹쳐** 보여 준다(`Tab` 또는 `→`로 확정). `SuggestAppend`: 둘 다 |
| `AutoCompleteSource` | 값은 보관되지만 후보는 **항상 목록(ListItems)** 에서 찾는다 — 콤보의 후보는 곧 바인딩된 항목이기 때문이다. `ListItems`/`CustomSource`/`None` 어느 값이어도 결과는 같다. `FileSystem`·`HistoryList` 같은 OS 원본은 미지원 |
| `AutoCompleteCustomSource` | **보관 전용**(레거시 코드 컴파일용). 후보를 바꾸려면 `DataSource`/`Items`를 바꾼다 |
| `Text` | 현재 선택/입력 텍스트 (읽기). 쓰기는 DropDown/Simple에서만 동작, DropDownList에서는 무동작 |
| `Enabled` | 전파됨 |

## 멀티컬럼 드롭다운

`ConfigureDropDownColumns(...)`로 드롭다운을 코드+명칭 같은 다중 컬럼으로 구성할 수 있다
(그리드와 동일한 `ModernDataGridColumn` 정의 재사용):

```csharp
using Modern.Lab.Controls.Wpf.Data;

this.cboDept.ConfigureDropDownColumns(
    new ModernDataGridColumn("DEPT_CODE", "코드", 60),
    new ModernDataGridColumn("DEPT_NAME", "부서명", 110));
this.cboDept.DisplayMember = "DEPT_NAME";   // 필드/선택 텍스트는 계속 명칭
this.cboDept.ValueMember = "DEPT_CODE";
this.cboDept.DataSource = deptTable;        // DataSource 할당 전에 구성
```

- 드롭다운 상단에 **컬럼 헤더 행** 표시, 폭은 컬럼 합계만큼 자동 확장
- 검색형(DropDown) 모드의 타이핑 필터는 **모든 컬럼 대상** — 코드("D3")로도
  명칭("개발", 초성 "ㄱ")으로도 필터링
- 선택 시 필드에는 `DisplayMember`(명칭)만 표시, `SelectedValue`는 그대로 코드
- 구성 후 단일 컬럼으로 되돌리는 것은 미지원 (폼 로드 시 1회 구성 전제)

## 계약 보장 동작 (docs/design-notes.md §6-1)

- `SelectedValue` → `DataSource` 순서로 설정해도 정상 동작 (보류 후 적용).
  수동 `Items` 컬렉션에서도 같은 규칙 — `SelectedValue` → `Items.Add` 순서면
  일치 항목이 추가되는 시점에 적용된다
- `DataSource` → `ValueMember` 순서로 설정해도 `SelectedValue`가 값을 돌려준다
  (원본 `ListControl`처럼 읽는 시점에 해석한다). `ValueMember`를 도중에 바꾸면
  같은 선택의 값이 새 컬럼 기준으로 즉시 바뀐다
- `DataSource` 재할당 시 선택이 깨끗하게 초기화되고 이벤트는 1회만 발생
- 보류 값이 없으면 첫 행 자동 선택 (WinForms `ComboBox` 기본 동작과 일치)
- null/빈 데이터는 예외 없이 표시하며 빈 드롭다운 팝업은 닫는다. `DropDown` 이벤트에서 동기적으로 목록을 채우는 방식은 유지한다. 열린 목록을 비워도 팝업이 닫힌다.
- `DropDown` 처리 중 목록을 비웠다가 다시 채우면 최종 목록으로 판단한다. 갱신 도중의 일시적인 0행 때문에 팝업을 닫지 않는다.
- 백그라운드 조회 후 UI 스레드 `Invoke` 할당 패턴 지원
- 타이핑으로 검색 목록을 열어도 입력 중 캐럿·선택 영역을 유지한다. 첫 글자를 자동으로 전체 선택하여 다음 글자에 덮이는 동작은 하지 않는다.

## 추가 멤버

| 멤버 | 설명 |
|---|---|
| `PlaceholderText` | 미선택/미입력 상태에서 표시할 힌트 텍스트 — **`ModernTextBox`와 동일한 속성명**. "전체" 더미 행 대신 미선택(`SelectedIndex = -1`) + 플레이스홀더 패턴 권장: `DataSource` 할당 후 `SelectedIndex = -1`로 초기화하면 미선택 상태가 유지되고, 폼 조회 코드에서 `SelectedValue == null`을 "전체"로 처리 |
| `Required` | 필수 입력 표시 — 값이 비어 있는 동안 필드 오른쪽에 빨간 점, 선택하면 사라짐 (입력 컨트롤 공통 속성) |
| `Highlight` | 강조 표시 — 필드에 액센트색 테두리를 덧그린다. 한 화면에서 특별히 주목이 필요한 핵심 선택 필드(예: 배정 대상 선택)에만 쓴다. `Required`와 별개로 함께 사용 가능 |
| `ItemColorPath` | **드롭다운 항목 글자색을 데이터 멤버로 지정** — 값으로 색 hex(`"#DC2626"` 등)를 담은 컬럼/속성 이름. 항목마다 상태를 색으로 구분할 때 쓴다(예: 채움 상태별 색). 비우면 기본색. 색은 **열린 목록 항목**에 적용되고 닫힌 필드 텍스트는 기본색이다 |
| `CharacterCasing` | 편집 가능 콤보(DropDown/Simple)에서 **입력 즉시 대문자/소문자 강제 변환** — `ModernTextBox`와 같은 이름·의미(WinForms `TextBox.CharacterCasing` 미러). Lot ID처럼 대문자만 존재하는 코드 검색 콤보에 `Upper`를 지정한다. 기본 `Normal` = 변환 없음. `DropDownList`(선택 전용)에서는 입력란이 없어 효과 없음 |

`AllowedCharacters`(string, 기본 `""`)는 편집 입력·붙여넣기·IME 입력에서 허용할 문자를 지정한다.
빈 문자열은 제한 없음이다. 예를 들어 `CharacterCasing = Upper`와
`AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-"`를 함께 지정하면
대문자·숫자·점·하이픈만 남고 한글과 다른 기호는 제거된다. 입력 위치는 유지하며
목록에서 선택한 표시값은 변형하지 않는다. 성명 검색처럼 한글이 필요하면 지정하지 않는다.

## 미지원 멤버와 대체 방법

| 기존 멤버 | 대체 |
|---|---|
| `Text` 쓰기로 항목 선택 (DropDownList) | `SelectedValue` 또는 `SelectedIndex`로 선택 |
| `AutoCompleteMode = Append`의 **텍스트 덧붙이기** | 원본은 입력란 텍스트에 나머지를 실제로 써 넣고 그 부분을 선택 상태로 둔다. 이 컨트롤은 **텍스트를 건드리지 않고 겹쳐 그리기만** 한다 — 한글 IME는 조합 중에 앞 글자가 계속 바뀌므로("그" → "글" → "그루") 그 시점에 버퍼를 수정하면 조합이 깨진다. 확정(`Tab`/`→`) 시점에만 실제 텍스트가 된다. 덕분에 **한글 완성문자에서도 영문과 똑같이** 힌트가 뜬다 |
| `AutoCompleteSource = FileSystem` 등 OS 원본 | 미지원 — 파일 경로 입력은 `ModernTextBox`의 자동완성을 쓴다 |
| `DropDownList` + `AutoCompleteMode = Suggest` | 원본은 이 조합에서도 목록을 띄우지만, 이 컨트롤은 선택 전용일 때 입력란 자체가 없어 효과가 없다. 타이핑으로 찾게 하려면 `DropDownStyle = DropDown`으로 둔다 |
| `SelectedValueChanged`/`SelectionChangeCommitted` | `SelectedIndexChanged`로 통합 |
| `FormattingEnabled`, `FormatString` | 미구현 — 표시 문자열은 데이터 쪽에서 가공 |
| `Font`, `BackColor`, `FlatStyle` | 없음 — 토큰이 결정 |

## .Designer.cs 교체 예시

```csharp
// 변경 전
private System.Windows.Forms.ComboBox cboDept;
this.cboDept = new System.Windows.Forms.ComboBox();
this.cboDept.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

// 변경 후
private Modern.Lab.WinForms.Controls.Selection.ModernComboBox cboDept;
this.cboDept = new Modern.Lab.WinForms.Controls.Selection.ModernComboBox();
```

폼의 서버 request/reply 코드는 그대로 둔다:

```csharp
// 기존 코드 변경 없음
this.cboDept.DisplayMember = "DEPT_NAME";
this.cboDept.ValueMember = "DEPT_CODE";
this.cboDept.SelectedValue = "D002";   // DataSource보다 먼저여도 동작
this.cboDept.DataSource = replyTable;  // 서버 응답 DataTable
```

권장 크기: 200×32. 높이 32는 기본(자동 배치의 요구 높이)이자 상한이다 — 호스트가 더 낮은 높이를 주면
(배율 125% 등 고DPI에서 스케일되지 않은 폼 포함) 필드가 그 높이로 줄어들며,
하단이 잘리지 않는다.

## 여러 개를 한 폼에 놓을 때 — 첫 표시 시간 (2026-09-02)

이 컨트롤은 WPF 섬(ElementHost)이다. 섬은 하나마다 GPU 렌더 타깃을 만드는데, 그 생성이 PC·그래픽
드라이버에 따라 **섬당 ~100ms**까지 든다(빈 `Border` 하나짜리 순정 ElementHost도 같다). 표 5개·KPI 카드 4장·
콤보 2개인 화면은 뜨는 데만 3초가 넘었다. 앱 시작 시(첫 폼 전) 한 줄로 소프트웨어 렌더링으로 고정하면
섬당 ~7~10ms다 — 업무 화면은 그림이 같고, RDP는 원래 소프트웨어로 그린다:

```csharp
Modern.Lab.WinForms.Controls.Hosting.WpfHostOptions.SoftwareRendering = true;
```

기본값은 off(켜지 않으면 기존과 같다). 자세한 것은 `controls-reference.md`의 "WPF 소프트웨어 렌더링".
