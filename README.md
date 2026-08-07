# 전자세금계산서 PDF → Excel 변환기

텍스트 레이어가 있는 전자세금계산서 PDF에서 적요, 공급가액, 부가세, 공급자/공급받는자 상호, 작성월일을 추출하고 사용자가 검토·수정한 뒤 `.xlsx`로 저장하는 Windows 업무용 프로그램입니다. 모든 처리는 로컬 PC에서 수행되며 PDF 본문을 외부로 전송하지 않습니다.

## 지원 환경과 기술 스택

- Windows 10/11 x64
- C# / .NET 8 / WinForms
- 배포: self-contained, 단일 실행 파일 `win-x64`
- PDF: PdfPig 0.1.15
- Excel: ClosedXML 0.105.1

WinForms는 이 규모의 Windows 전용 업무 UI를 단순하게 유지하고 self-contained 배포하기에 적합합니다. PdfPig는 단어와 bounding box 좌표를 제공하므로 공급자와 공급받는자 영역을 구분할 수 있습니다. ClosedXML은 Office Interop 없이 Excel 미설치 PC에서도 `.xlsx`를 생성합니다.

## 외부 라이브러리와 라이선스

- [PdfPig](https://github.com/UglyToad/PdfPig): Apache License 2.0. PDF 텍스트, 단어 및 좌표 추출에 사용합니다.
- [ClosedXML](https://github.com/ClosedXML/ClosedXML): MIT License. `.xlsx` 생성과 서식에 사용합니다.
- 테스트 전용 xUnit: Apache License 2.0

위 라이선스들은 상업적 이용을 허용하지만, 배포 시 각 패키지의 저작권/라이선스 고지를 함께 보존해야 합니다. 실제 배포 전 조직의 라이선스 정책에 따른 최종 검토를 권장합니다.

## 프로젝트 구조

```text
TaxInvoiceExtractor/
├─ src/TaxInvoiceExtractor/
│  ├─ UI/               WinForms 화면과 사용자 상호작용
│  ├─ Models/           PDF 목록과 추출 결과 모델
│  ├─ Pdf/              PDF 좌표 추출, 필드 추출, 파서
│  ├─ Services/         다중 PDF 처리와 Excel 저장
│  ├─ Utils/            숫자·날짜 정규화와 검증
│  └─ Logging/          로컬 최소 로그
├─ tests/               정규화, 좌우 영역, Excel 테스트
└─ build-release.ps1    self-contained Release 생성
```

## 개발환경에서 실행

`.NET 8 SDK`가 필요합니다.

```powershell
dotnet restore .\TaxInvoiceExtractor.sln
dotnet test .\TaxInvoiceExtractor.sln
dotnet run --project .\src\TaxInvoiceExtractor\TaxInvoiceExtractor.csproj
```

## Release Build

```powershell
.\build-release.ps1
```

결과는 `release\win-x64\전자세금계산서변환기.exe`에 생성됩니다. self-contained 단일 파일이므로 대상 PC에 .NET SDK나 Runtime이 필요하지 않습니다. 첫 실행 시 단일 파일에 포함된 일부 네이티브 구성 요소가 임시 폴더로 추출될 수 있습니다.

## 사용 방법

1. `PDF 폴더 불러오기`를 눌러 폴더를 선택하면 해당 폴더 바로 아래의 PDF 전체를 파일명 순서로 등록합니다. PDF 파일 끌어놓기도 지원합니다.
2. 위로/아래로 버튼으로 순서를 정합니다. 이 순서가 Excel의 순번이 됩니다.
3. `데이터 추출`을 눌러 진행 상태를 확인합니다.
4. 결과 표에서 값을 검토하고 필요한 셀을 직접 수정합니다.
5. `Excel로 저장`을 눌러 위치와 파일명을 선택합니다.

처리 로그는 실행 파일 옆 `logs\yyyyMMdd.log`에 기록됩니다. PDF 본문과 추출된 거래 내용 전체는 로그에 기록하지 않습니다.

## PDF Parser 구조

`IPdfTextExtractor`가 PDF 읽기를 추상화하고 `PdfTextExtractor`가 각 단어의 bounding box를 페이지 너비/높이 기준 0~1 좌표로 정규화합니다. `FieldExtractor`는 상호 필드에 대해 페이지 왼쪽 공급자 영역과 오른쪽 공급받는자 영역을 독립적으로 탐색합니다. 금액과 날짜는 라벨의 같은 행과 인접 좌표를 우선 사용합니다. `TaxInvoiceParser`는 추출 실패 필드를 임의 값으로 채우지 않고 `확인 필요`와 오류 메시지로 반환합니다.

이 구조는 단순 전체 본문 정규식 파서가 아닙니다. 다만 전자세금계산서 발행 시스템별 PDF 좌표·라벨 구조가 다를 수 있으므로 실제 사용 문서에 맞춘 `FieldExtractor` 조정이 필요할 수 있습니다.

## Excel Export 구조

ClosedXML로 제목 굵게, 자동 필터, 첫 행 고정, 테두리, 열 너비, 금액 숫자 타입과 `#,##0` 표시 형식을 적용합니다. Microsoft Excel이 설치되어 있지 않아도 파일을 생성할 수 있습니다.

## 알려진 제한사항

- 현재 제공된 작업 자료에는 실제 전자세금계산서 PDF 샘플이 없어 특정 발행 양식에 대한 추출 정확도는 검증하지 못했습니다.
- 텍스트 레이어가 없는 스캔 PDF와 이미지 PDF의 OCR은 1차 버전에서 지원하지 않습니다.
- 암호화/손상 PDF, 특수하게 글자가 개별 경로로 그려진 PDF는 읽지 못할 수 있습니다.
- 회사명이나 품목이 라벨과 다른 행에 복잡하게 배치된 양식은 규칙 보강이 필요할 수 있습니다.
- 실제 Windows 10/11 별도 PC에서의 실행 확인은 해당 테스트 PC에서 수행해야 합니다.

## 향후 확장

- `IPdfTextExtractor` 구현으로 OCR 엔진 추가
- 발행 시스템별 양식 프로필과 자동 감지
- 실제 샘플 기반 다중 품목 병합 규칙 정교화
- `win-arm64` 별도 Release
- 좌표 덤프/진단 화면을 통한 새 양식 온보딩

실제 샘플을 추가할 때는 개인정보가 포함될 수 있으므로 안전한 로컬 작업 공간에만 두고 저장소에는 커밋하지 않는 것을 권장합니다.
