# 현재 전달분 — 2026-09-24

**회사 반영 상태: 반영 대기**

- 범위: 기존 반영 대기 13개와 이후 Reason/Common Code 변경을 합친 최신 소스 22개. 2026-09-23 16:30 KST는 사용자가 알려준 잠정 기준이며, 회사 사본과 직접 비교한 결과는 아니다.
- 원본 기준 커밋: `bd0b1e6d1a075d9fd3e0e940edfa8f870b8f1aa2` (`main`). 검토 완료된 기본값 선언과 저장 후 재선택 수정까지 원본에 커밋·푸시했으며, 아래 소스 22개는 그 원본과 일치한다.
- 이번에 갱신한 기존 전달 파일은 `CommonCodeForm.cs`이고, 추가로 전달하는 파일은 Hosting 3개·Common Code 3개·Reason Code 3개다. 나머지 반영 대기 12개는 계속 포함한다.
- 회사에서 삭제할 파일: 없음. 이 22개는 기존 회사 파일의 교체 대상이며, Public의 파일 정리는 회사 소스 삭제 지시가 아니다.
- 테스트·Home 코드·AI 설정·검토 문서는 포함하지 않는다.

## 적용 파일

경로는 원본 저장소 기준이다. Commons는 공통 컨트롤, Hosting은 회사 상위 Common, Samples와 MasterData는 해당 업무 프로젝트에 대응한다.

| 구분 | 파일 경로 | 변경 요약 |
|---|---|---|
| 공통 컨트롤 | [Modern.Lab.Commons/Controls/Wpf/Selection/ModernComboBoxControl.xaml.cs](Modern.Lab.Commons/Controls/Wpf/Selection/ModernComboBoxControl.xaml.cs) | 허용 문자와 빈 목록·드롭다운 처리 |
| 공통 컨트롤 | [Modern.Lab.Commons/WinForms/Selection/ModernComboBox.cs](Modern.Lab.Commons/WinForms/Selection/ModernComboBox.cs) | AllowedCharacters 속성 노출 |
| 상위 Common | [Modern.Lab.Hosting/ModernFormBase.Messaging.cs](Modern.Lab.Hosting/ModernFormBase.Messaging.cs) | 조회 결과 없음 응답 처리 |
| 상위 Common | [Modern.Lab.Hosting/MasterData/MasterDataCrudDefinition.cs](Modern.Lab.Hosting/MasterData/MasterDataCrudDefinition.cs) | CRUD 복합 키 정의 |
| 상위 Common | [Modern.Lab.Hosting/MasterData/MasterDataCrudFormBase.cs](Modern.Lab.Hosting/MasterData/MasterDataCrudFormBase.cs) | 복합 키 CRUD·신규 입력·저장 후 재선택 |
| 상위 Common | [Modern.Lab.Hosting/MasterData/ModernPropertyGrid.cs](Modern.Lab.Hosting/MasterData/ModernPropertyGrid.cs) | 신규 기본값 선언과 기존 조회 값 보존 |
| 업무 화면 | [Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.Grid.cs](Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.Grid.cs) | 코드 콤보 입력 제한 |
| 업무 화면 | [Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.cs](Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.cs) | 빈 조회 처리와 콤보 표시·값 설정 재사용 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.cs](Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.cs) | 공통 CRUD 베이스 적용·부모 값 전달·기존 키 원값 재선택 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Designer.cs](Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Designer.cs) | 공통 CRUD 베이스 화면 구성 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Server.cs](Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Server.cs) | 기존 키 원값으로 수정·삭제, 신규 키만 공백 제거 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Grid.cs](Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.Grid.cs) | SORT_NO=0·USE_YN=Y 신규 기본값 선언 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.cs](Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.cs) | 복합 키 공통 CRUD 베이스 적용 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.Server.cs](Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.Server.cs) | 기존 키 원값으로 수정·삭제, 신규 키만 공백 제거 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.Grid.cs](Modern.Lab.MasterData/Screens/ReasonCodes/ReasonCodeForm.Grid.cs) | 키·편집기 설정을 공통 CRUD 구조에 맞춤 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Equipment/EquipmentLotForm.cs](Modern.Lab.Samples/Screens/Equipment/EquipmentLotForm.cs) | 목록 재조회 중 불필요한 조회 방지와 미사용 멤버 정리 |
| 업무 계약 | [Modern.Lab.Samples/Contracts/EquipmentLotContracts.cs](Modern.Lab.Samples/Contracts/EquipmentLotContracts.cs) | 미사용 멤버 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Equipment/EquipmentPortManagementForm.cs](Modern.Lab.Samples/Screens/Equipment/EquipmentPortManagementForm.cs) | 미사용 상수 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Lots/LotManagementForm.cs](Modern.Lab.Samples/Screens/Lots/LotManagementForm.cs) | 미사용 상수 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Durables/DurableManagementForm.cs](Modern.Lab.Samples/Screens/Durables/DurableManagementForm.cs) | 미사용 조회 메서드 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Durables/CarrierEditForm.ContractState.cs](Modern.Lab.Samples/Screens/Durables/CarrierEditForm.ContractState.cs) | 미사용 조회 메서드 정리 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/PfoNodes/PfoNodeForm.cs](Modern.Lab.MasterData/Screens/PfoNodes/PfoNodeForm.cs) | 취소 시 불필요한 복사와 미사용 필드 정리 |

## 적용 참고

- Commons를 DLL로 사용하는 회사 프로젝트는 이 소스를 반영한 DLL로 갱신해야 한다. 소스만 복사하면 기존 DLL의 동작은 바뀌지 않는다.
- Hosting은 회사 상위 Common의 소스다. Reason/Common 변경에는 Hosting의 MasterData 3개와 각 화면 파일을 함께 반영한다. Common의 고정 신규 기본값은 `.Grid.cs`에서 수정하고, 선택한 부모 값은 폼에서 전달한다.
- 회사에서 별도로 수정한 파일은 변경 내용을 비교하여 반영한다. 회사 사본의 동작·빌드는 이번 전달 확인 범위에 포함하지 않았다.
- 미사용 멤버 정리가 포함된 파일은 회사 적용 후 해당 프로젝트 빌드로 참조 오류를 확인한다. 관련 오류가 없으면 별도 검색은 필요 없다.
- 전달 소스 22개가 원본 작업 트리와 SHA-256 기준으로 일치하는지 확인했다. 전달 과정에서 빌드·전체 회귀 검사는 반복하지 않았다. 직전 검토에서는 관련 기존 검사 결과 581/0을 확인하고, 신규 기본값 경계 14건을 별도 실행해 14/0을 확인했다.

## 회사 반영 기록

| 전달분 | 회사 반영 상태 |
|---|---|
| 2026-09-24 / 위 22개 파일 | 반영 대기 — 아직 완료 확인을 받지 않음 |
