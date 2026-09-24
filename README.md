# 현재 전달분 — 2026-09-24

**회사 반영 상태: 반영 대기**

- 범위: 2026-09-23 16:30 KST 이후 변경한 소스 13개. 사용자가 알려준 시각은 잠정 기준이며, 회사 사본과 직접 비교한 결과는 아니다.
- 원본 기준 커밋: `68a6280e2695ca2fd96d1fab9a0a84c8e7091e63`.
- 이번 목록은 17:14 전달분에서 이후 Public 정리로 빠졌던 파일과 후속 변경을 합친 최신본이다.
- 회사에서 삭제할 파일: 없음. 기존 파일 13개의 수정본이며, 과거 Public의 파일 삭제는 회사 소스 삭제 지시가 아니다.
- 테스트·Home 코드·AI 설정·검토 문서는 포함하지 않는다.

## 적용 파일

경로는 원본 저장소 기준이다. Commons는 공통 컨트롤, Hosting은 회사 상위 Common, Samples와 MasterData는 해당 업무 프로젝트에 대응한다.

| 구분 | 파일 경로 | 변경 요약 |
|---|---|---|
| 공통 컨트롤 | [Modern.Lab.Commons/Controls/Wpf/Selection/ModernComboBoxControl.xaml.cs](Modern.Lab.Commons/Controls/Wpf/Selection/ModernComboBoxControl.xaml.cs) | 허용 문자와 빈 목록·드롭다운 처리 |
| 공통 컨트롤 | [Modern.Lab.Commons/WinForms/Selection/ModernComboBox.cs](Modern.Lab.Commons/WinForms/Selection/ModernComboBox.cs) | AllowedCharacters 속성 노출 |
| 상위 Common | [Modern.Lab.Hosting/ModernFormBase.Messaging.cs](Modern.Lab.Hosting/ModernFormBase.Messaging.cs) | 조회 결과 없음 응답 처리 |
| 업무 화면 | [Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.Grid.cs](Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.Grid.cs) | 코드 콤보 입력 제한 |
| 업무 화면 | [Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.cs](Modern.Lab.Samples/Dialogs/Lots/LotHoldDialogForm.cs) | 빈 조회 처리와 콤보 표시·값 설정 재사용 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.cs](Modern.Lab.MasterData/Screens/CommonCodes/CommonCodeForm.cs) | 콤보 표시·값 설정 재사용 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Equipment/EquipmentLotForm.cs](Modern.Lab.Samples/Screens/Equipment/EquipmentLotForm.cs) | 목록 재조회 중 불필요한 조회 방지와 미사용 멤버 정리 |
| 업무 계약 | [Modern.Lab.Samples/Contracts/EquipmentLotContracts.cs](Modern.Lab.Samples/Contracts/EquipmentLotContracts.cs) | 미사용 멤버 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Equipment/EquipmentPortManagementForm.cs](Modern.Lab.Samples/Screens/Equipment/EquipmentPortManagementForm.cs) | 미사용 상수 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Lots/LotManagementForm.cs](Modern.Lab.Samples/Screens/Lots/LotManagementForm.cs) | 미사용 상수 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Durables/DurableManagementForm.cs](Modern.Lab.Samples/Screens/Durables/DurableManagementForm.cs) | 미사용 조회 메서드 정리 |
| 업무 화면 | [Modern.Lab.Samples/Screens/Durables/CarrierEditForm.ContractState.cs](Modern.Lab.Samples/Screens/Durables/CarrierEditForm.ContractState.cs) | 미사용 조회 메서드 정리 |
| 마스터데이터 | [Modern.Lab.MasterData/Screens/PfoNodes/PfoNodeForm.cs](Modern.Lab.MasterData/Screens/PfoNodes/PfoNodeForm.cs) | 취소 시 불필요한 복사와 미사용 필드 정리 |

## 적용 참고

- Commons를 DLL로 사용하는 회사 프로젝트는 이 소스를 반영한 DLL로 갱신해야 한다. 소스만 복사하면 기존 DLL의 동작은 바뀌지 않는다.
- 회사에서 별도로 수정한 파일은 변경 내용을 비교하여 반영한다. 회사 사본의 동작·빌드는 이번 전달 확인 범위에 포함하지 않았다.
- 이번 전달에서는 소스 13개가 원본 최신본과 일치하는지 확인했다. 빌드·회귀 검사는 재실행하지 않았다.

## 회사 반영 기록

| 전달분 | 회사 반영 상태 |
|---|---|
| 2026-09-24 / 위 13개 파일 | 반영 대기 — 아직 완료 확인을 받지 않음 |
