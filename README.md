# 현재 전달분 — 2026-09-26

**회사 반영 상태: 반영 대기**

- 범위: 2026-09-25 전달분 25개 + User 화면 누락분 3개 + 이벤트 연결 이동 9개 = 37개.
- 원본 기준 커밋: `4a2fb365` (`main`). 아래 소스 37개는 그 원본과 일치한다.
- 2026-09-26 전달은 세 번이다.
  1. VS 디자이너 오류 수정 3개 파일 교체 — `MasterDataCrudFormBase.cs`·`CommonCodeForm.cs`·`CommonCodeForm.Designer.cs`.
  2. User 화면 누락분 3개 파일 **추가** — `UserForm.Designer.cs`·`UserForm.Grid.cs`·`UserForm.Server.cs`. 회사에는 User 화면이 없는데
     `UserForm.cs`만 전달돼 `InitializeComponent`·`gridUsers`를 찾지 못했다.
  3. **공통 이벤트 연결을 베이스로** — 13개 파일(아래 「2026-09-26 이벤트 연결」). 1번 뒤 디자인 모드에서
     "The method 'OnNewClick' cannot be the method for an event because a class this class derives from already defines the method"가 나던 것을 고친다.
  이미 반영 중인 나머지는 그대로 두고 위 파일만 바꾸거나 추가한다.
- 회사에서 삭제할 파일: 없음. User 화면 3개는 새로 추가하는 파일이고 나머지는 기존 회사 파일의 교체 대상이다. Public의 파일 정리는 회사 소스 삭제 지시가 아니다.
- 테스트·Home 코드·AI 설정·검토 문서는 포함하지 않는다.

## 적용 파일

경로는 원본 저장소 기준이다. Commons는 공통 컨트롤, Hosting은 회사 상위 Common, Samples와 MasterData는 해당 업무 프로젝트에 대응한다.

| 구분 | 파일 경로 | 변경 요약 |
|---|---|---|
| 공통 컨트롤 | [Modern.Lab.Commons/Controls/Wpf/Selection/ModernComboBoxControl.xaml.cs](Modern.Lab.Commons/Controls/Wpf/Selection/ModernComboBoxControl.xaml.cs) | 허용 문자와 빈 목록·드롭다운 처리 |
| 공통 컨트롤 | [Modern.Lab.Commons/WinForms/Selection/ModernComboBox.cs](Modern.Lab.Commons/WinForms/Selection/ModernComboBox.cs) | AllowedCharacters 속성 노출 |
| 상위 Common | [Modern.Lab.Hosting/ModernFormBase.Messaging.cs](Modern.Lab.Hosting/ModernFormBase.Messaging.cs) | 조회 결과 없음 응답 처리 |
| 상위 Common | [Modern.Lab.Hosting/ModernFormBase.cs](Modern.Lab.Hosting/ModernFormBase.cs) | 쓰기 시작·종료 상태 알림 |
| 상위 Common | [Modern.Lab.Hosting/MasterData/MasterDataCrudView.cs](Modern.Lab.Hosting/MasterData/MasterDataCrudView.cs) | 쓰기 중 잠글 조회 버튼 연결 |
| 상위 Common | [Modern.Lab.Hosting/MasterData/MasterDataCrudDefinition.cs](Modern.Lab.Hosting/MasterData/MasterDataCrudDefinition.cs) | CRUD 복합 키 정의 |
| 상위 Common | [Modern.Lab.Hosting/MasterData/MasterDataCrudFormBase.cs](Modern.Lab.Hosting/MasterData/MasterDataCrudFormBase.cs) | 복합 키 CRUD·신규 입력·재선택·쓰기 중 입력 잠금. **2026-09-26** 디자이너가 열 수 있도록 `abstract` 제거, 공통 이벤트를 베이스가 연결 |
| 상위 Common | [Modern.Lab.Hosting/MasterData/ModernPropertyGrid.cs](Modern.Lab.Hosting/MasterData/ModernPropertyGrid.cs) | 신규 기본값 선언과 기존 조회 값 보존 |
| 업무 화면 | [Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.Grid.cs](Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.Grid.cs) | 코드 콤보 입력 제한 |
| 업무 화면 | [Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.cs](Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.cs) | 빈 조회 처리와 콤보 표시·값 설정 재사용 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.cs](Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.cs) | 공통 CRUD·기존 키 원값 재선택·중복 잠금 제거, 대분류 입력은 폼에서 잠금. **2026-09-26** 새 컨트롤 필드 사용, Common Type 표시는 `ApplyScreenMode()` |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Designer.cs](Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Designer.cs) | 공통 CRUD 베이스 화면 구성. **2026-09-26** VS 디자이너가 여는 표준 생성 형식으로 다시 씀 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Server.cs](Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Server.cs) | 기존 키 원값으로 수정·삭제, 신규 키만 공백 제거 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Grid.cs](Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Grid.cs) | SORT_NO=0·USE_YN=Y 신규 기본값 선언 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.cs](Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.cs) | 복합 키 공통 CRUD 적용·중복 잠금 제거 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.Server.cs](Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.Server.cs) | 기존 키 원값으로 수정·삭제, 신규 키만 공백 제거 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.Grid.cs](Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.Grid.cs) | 키·편집기 설정을 공통 CRUD 구조에 맞춤 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Equipment/EquipmentLotForm.cs](Modern.Lab.Samples/Screens/Equipment/EquipmentLotForm.cs) | 목록 재조회 중 불필요한 조회 방지와 미사용 멤버 정리 |
| 업무 계약 | [Modern.Lab.Samples/Contracts/EquipmentLotContracts.cs](Modern.Lab.Samples/Contracts/EquipmentLotContracts.cs) | 미사용 멤버 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Equipment/EquipmentPortManagementForm.cs](Modern.Lab.Samples/Screens/Equipment/EquipmentPortManagementForm.cs) | 미사용 상수 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Lots/LotManagementForm.cs](Modern.Lab.Samples/Screens/Lots/LotManagementForm.cs) | 미사용 상수 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Durables/DurableManagementForm.cs](Modern.Lab.Samples/Screens/Durables/DurableManagementForm.cs) | 미사용 조회 메서드 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Durables/CarrierEditForm.ContractState.cs](Modern.Lab.Samples/Screens/Durables/CarrierEditForm.ContractState.cs) | 미사용 조회 메서드 정리 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/PfoNodes/PfoNodeForm.cs](Modern.Lab.MasterData/Screens/PfoNodes/PfoNodeForm.cs) | 취소 시 불필요한 복사와 미사용 필드 정리 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/Users/UserForm.cs](Modern.Lab.MasterData/Screens/Users/UserForm.cs) | 폼별 대기 잠금을 제거하고 공통 잠금 사용 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/Users/UserForm.Designer.cs](Modern.Lab.MasterData/Screens/Users/UserForm.Designer.cs) | **2026-09-26 추가** — User 화면 구성(`InitializeComponent`·`gridUsers` 등) |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/Users/UserForm.Grid.cs](Modern.Lab.MasterData/Screens/Users/UserForm.Grid.cs) | **2026-09-26 추가** — 편집기 설정(`DefineEditors`) |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/Users/UserForm.Server.cs](Modern.Lab.MasterData/Screens/Users/UserForm.Server.cs) | **2026-09-26 추가** — 조회·등록·수정·삭제 전문과 응답 필수 컬럼(`USER_ID`) |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/AreaBays/AreaBayForm.Designer.cs](Modern.Lab.MasterData/Screens/AreaBays/AreaBayForm.Designer.cs) | **2026-09-26** 공통 이벤트 줄 삭제, 데모 안내 라벨을 필드로 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/EquipmentGroups/EquipmentGroupForm.Designer.cs](Modern.Lab.MasterData/Screens/EquipmentGroups/EquipmentGroupForm.Designer.cs) | **2026-09-26** 공통 이벤트 줄 삭제 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/Flows/FlowForm.Designer.cs](Modern.Lab.MasterData/Screens/Flows/FlowForm.Designer.cs) | **2026-09-26** 공통 이벤트 줄 삭제 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/LotCodes/LotCodeForm.Designer.cs](Modern.Lab.MasterData/Screens/LotCodes/LotCodeForm.Designer.cs) | **2026-09-26** 공통 이벤트 줄 삭제 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/Opers/OperForm.Designer.cs](Modern.Lab.MasterData/Screens/Opers/OperForm.Designer.cs) | **2026-09-26** 공통 이벤트 줄 삭제 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/Ports/PortForm.cs](Modern.Lab.MasterData/Screens/Ports/PortForm.cs) | **2026-09-26** 장비 먼저 불러오기·장비 선택 조회를 `OnFormLoad`·`OnSearchClick`·`OnKeywordEnterPressed` 재정의로 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/Ports/PortForm.Designer.cs](Modern.Lab.MasterData/Screens/Ports/PortForm.Designer.cs) | **2026-09-26** 공통 이벤트·조회·로드 줄 삭제 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/Products/ProductForm.Designer.cs](Modern.Lab.MasterData/Screens/Products/ProductForm.Designer.cs) | **2026-09-26** 공통 이벤트 줄 삭제 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.Designer.cs](Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.Designer.cs) | **2026-09-26** 공통 이벤트 줄 삭제 |

## 적용 참고

- Commons를 DLL로 사용하는 회사 프로젝트는 이 소스를 반영한 DLL로 갱신해야 한다. 소스만 복사하면 기존 DLL의 동작은 바뀌지 않는다.
- Hosting은 회사 상위 Common의 소스다. Reason/Common 변경에는 Hosting의 MasterData 3개와 각 화면 파일을 함께 반영한다. Common의 고정 신규 기본값은 `.Grid.cs`에서 수정하고, 선택한 부모 값은 폼에서 전달한다.
- 쓰기 중 잠금 변경은 Hosting `ModernFormBase.cs`·`MasterDataCrudFormBase.cs`·`MasterDataCrudView.cs`와 업무 화면 `UserForm.cs`·`ReasonCodeForm.cs`·`CommonCodeForm.cs` 6개를 함께 반영한다. 이번 잠금 변경 자체는 Commons DLL 교체가 필요 없지만, 기존 대기분의 콤보 수정에는 위 DLL 갱신이 필요하다.
- 2026-09-26 디자이너 수정: `MasterDataCrudFormBase`가 `abstract`여서 이를 상속한 CRUD 폼이 디자인 모드에서 열리지 않았다. `RequestItems`·`InsertItem`·`UpdateItem`은 가상 메서드가 됐고 기존 화면의 `override`는 그대로 컴파일된다. `CommonCodeForm`의 `editorPane`은 없어졌다 — 회사 partial이 참조했다면 `txtKeyword`·`btnSearch`·`gridItems`·`propertyGrid`·`btnNew`·`btnCancel`·`btnSave`·`btnDelete`로 바꾼다. 화면 동작과 레이아웃은 같다.
- 2026-09-26 이벤트 연결: 원인은 CRUD 폼 Designer.cs가 버튼 이벤트를 베이스 메서드(`OnNewClick` 등)에 연결한 것이다. VS 디자이너는 폼 자신의 소스에 있는 메서드만 핸들러로 받는다. 이제 `MasterDataCrudFormBase.InitializeCrud`가 이름으로 찾은 `txtKeyword`·`btnSearch`·`btnNew`·`btnCancel`·`btnSave`·`btnDelete`·`menuActions`·`miNew`·`miCancel`·`miSave`·`miDelete`와 폼 `Load`에 직접 연결한다. 적용:
  - Hosting `MasterDataCrudFormBase.cs`를 교체한다. 옛 Designer에 같은 줄이 남아 있어도 베이스가 한 번만 붙이므로 먼저 반영해도 실행 동작은 같다.
  - CRUD 폼 10개의 `.Designer.cs`에서 베이스 메서드를 가리키는 `+=` 줄을 지운다 — `OnKeywordEnterPressed`·`OnSearchClick`·`OnNewClick`·`OnCancelClick`·`OnSaveClick`·`OnDeleteClick`·`OnActionMenuOpening`·`OnFormLoad`(Port는 `OnPortSearch`·`OnPortLoad`, Common Code는 `OnCommonCodeLoad`도). 회사가 Designer 레이아웃을 고쳐 썼다면 파일을 교체하지 말고 그 줄만 지운다. 그 화면만의 핸들러(목록 선택 변경 등)는 남긴다.
  - `PortForm.cs`·`CommonCodeForm.cs`는 조회·첫 로드를 재정의로 옮겼으므로 교체한다(옛 Designer가 `OnPortSearch`·`OnPortLoad`·`OnCommonCodeLoad`를 가리키면 컴파일 오류로 알 수 있다).
  - 속성 창의 이벤트 탭에는 이 버튼들의 핸들러가 비어 보인다. 연결은 베이스에 있다.
- 2026-09-26 User 화면 추가: 회사 `.csproj`에 `UserForm.cs`는 `<SubType>Form</SubType>`로, `UserForm.Designer.cs`·`.Grid.cs`·`.Server.cs`는 `<DependentUpon>UserForm.cs</DependentUpon>`로 등록한다. 네 파일의 네임스페이스는 같아야 한다(원본 `Modern.Lab.MasterData`). `.Server.cs`의 전문·필드 이름은 홈 데모 값이므로 회사 전문에 맞춰 고친다.
- 회사에서 별도로 수정한 파일은 변경 내용을 비교하여 반영한다. 회사 사본의 동작·빌드는 이번 전달 확인 범위에 포함하지 않았다.
- 미사용 멤버 정리가 포함된 파일은 회사 적용 후 해당 프로젝트 빌드로 참조 오류를 확인한다. 관련 오류가 없으면 별도 검색은 필요 없다.
- 전달 소스가 원본 기준 커밋과 일치하는지 확인했다(2026-09-26 기준 37개, git blob 해시 비교). 전달 과정에서 빌드·전체 회귀 검사는 반복하지 않았다. 쓰기 중 잠금 검토에서는 관련 기존 검사 결과 791/0을 확인하고, 액션 종료·잠금 복원 경계 23건을 별도 실행해 23/0을 확인했다.

## 회사 반영 기록

| 전달분 | 회사 반영 상태 |
|---|---|
| 2026-09-25 / 위 25개 파일 | 반영 중 — 아직 완료 확인을 받지 않음 |
| 2026-09-26 / 디자이너 수정 3개 파일 교체 | 반영 대기 |
| 2026-09-26 / User 화면 누락분 3개 파일 추가 | 반영 대기 |
| 2026-09-26 / 공통 이벤트 연결 베이스 이동 13개 파일 | 반영 대기 |
