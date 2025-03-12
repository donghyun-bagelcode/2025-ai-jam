using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System;
using Dunward.Capricorn;

public class PromptManager
{
    private static List<string> selectedFiles = new List<string>();

    public static string AddFilesToUserPrompt(string userInput)
    {
        selectedFiles = FileSelectorWindow.LoadSelectedFiles();

        if (selectedFiles.Count == 0)
        {
            return userInput;
        }

        List<FileStatisticsCsv> fileInfoList = new();

        foreach (string file in selectedFiles)
        {
            try
            {
                string filename = Path.GetFileNameWithoutExtension(file) + ".csv";
                string json = File.ReadAllText(file);
                string csv = JsonToCsvConverter.JsonToCsvConverterMethod(json);
                //Debug.Log($"File: {file}, Filename: {filename}");

                JsonStatistics stats = JsonStatisticsCalculator.CalculateStatistics(json);

                fileInfoList.Add(new FileStatisticsCsv
                {
                    Filename = filename,
                    Statistics = stats,
                    CsvData = csv
                
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while processing file: {file}. Error: {ex.Message}");
                continue;
            }
        }

        string scriptPrompt = ScriptPromptFormat(fileInfoList);

        return userInput + scriptPrompt;
    }

    public static string ScriptPromptFormat(List<FileStatisticsCsv> fileInfoList)
    {
        StringBuilder scriptPromptBuilder = new StringBuilder();

        scriptPromptBuilder.AppendLine("\n\n**아래는 스토리의 컨텍스트를 위해 첨부된 CSV 노드 형식의 비주얼 노벨 스토리 데이터 입니다. 주로 이전 씬 정보를 담고 있으며, 소설의 스토리 작성시 참고할 수 있습니다.:**");

        foreach (var fileInfo in fileInfoList)
        {
            scriptPromptBuilder.AppendLine($"\n**File:** {fileInfo.Filename}");

            // // 통계값 추가
            // scriptPromptBuilder.AppendLine("**Statistics:**");
            // scriptPromptBuilder.AppendLine($"- Max node id: {fileInfo.Statistics.MaxId}");
            // scriptPromptBuilder.AppendLine($"- Max x: {fileInfo.Statistics.MaxX}");
            // scriptPromptBuilder.AppendLine($"- Min x: {fileInfo.Statistics.MinX}");
            // scriptPromptBuilder.AppendLine($"- Max y: {fileInfo.Statistics.MaxY}");
            // scriptPromptBuilder.AppendLine($"- Min y: {fileInfo.Statistics.MinY}");

            // CSV 데이터 추가
            scriptPromptBuilder.AppendLine("\n**CSV nodes Data:**");
            scriptPromptBuilder.AppendLine($"{fileInfo.Filename}");
            scriptPromptBuilder.AppendLine("```csv");
            scriptPromptBuilder.AppendLine(fileInfo.CsvData);
            scriptPromptBuilder.AppendLine("```");
        }

        return scriptPromptBuilder.ToString();
    }

    public static string DirectingPrompt(string csvContents, string commentary)
    {
        List<string> backgrounds = Resources.Load<BackgroundDatabase>("BackgroundDatabase").backgrounds.Keys.ToList();
        List<string> characters = Resources.Load<CharacterDatabase>("CharacterDatabase").characters.Keys.ToList();
        List<string> bgms = Resources.Load<AudioDatabase>("AudioDatabase").bgms.Keys.ToList();
        List<string> sfxs = Resources.Load<AudioDatabase>("AudioDatabase").sfxs.Keys.ToList();

        StringBuilder directingPromptBuilder = new StringBuilder();
        directingPromptBuilder.AppendLine("!!Note : Use only the following resources in your script!!");
        directingPromptBuilder.AppendLine("\n\n**Available Resources:**");
        directingPromptBuilder.AppendLine($"\n\nBackgrounds: {String.Join(", ", backgrounds)}");
        directingPromptBuilder.AppendLine($"\n\nCharacters: {String.Join(", ", characters)}");
        directingPromptBuilder.AppendLine($"\n\nBGMs: {String.Join(", ", bgms)}");
        directingPromptBuilder.AppendLine($"\n\nSFXs: {String.Join(", ", sfxs)}");

        if (!string.IsNullOrEmpty(commentary))
        {
            directingPromptBuilder.AppendLine("\n\n**Directing Commentary From User:**");
            directingPromptBuilder.AppendLine(commentary);
        }


        directingPromptBuilder.AppendLine("\n\n**CSV Contents:**");
        directingPromptBuilder.AppendLine("```csv");
        directingPromptBuilder.AppendLine(csvContents);
        directingPromptBuilder.AppendLine("```");

        
        // Debug.Log(String.Join(',',backgrounds));
        // Debug.Log(String.Join(',',characters));
        // Debug.Log(String.Join(',',bgms));
        // Debug.Log(String.Join(',',sfxs));

        return directingPromptBuilder.ToString();

    }
    public struct FileStatisticsCsv
    {
        public string Filename { get; set; }
        public JsonStatistics Statistics { get; set; }
        public string CsvData { get; set; }
    }

    public static string Completion = $@"
캐릭터 프로필을 생성해줘. 각 카테고리별로 아래 형식에 맞춰서 출력해줘. JSON 형식으로 출력하되, 각 필드를 개별적으로 업데이트할 수 있도록 키-값 쌍을 순차적으로 제공해줘.

{mainProfile}

";

    public static string Logline = @"
장르와 키워드를 바탕으로 주요 인물, 갈등, 목표를 조합하여 이야기의 핵심을 한 문장으로 요약된 로그라인을 생성합니다. 메인 캐릭터는 최대 3인이 등장 가능합니다.

출력 형식:
{키워드 및 장르에서 영감을 받은 인물과 그들의 상황을 소개하고, 주요 갈등과 목표를 한 문장으로 설명합니다. 톤을 반영하여 이야기를 매력적으로 요약합니다.}

Examples:
입력 예시:
장르 : 공상과학, 스릴러, 판타지, 코미디
키워드 : 고대문명, 신인류, 유적, 첨단과학

출력 예시:
고대문명의 유적을 탐사하던 중, 신인류의 존재를 암시하는 유물을 발견한 천재 과학자 지훈과 유적을 지키려는 고대 문명의 후손인 수호자 민아가 첨단과학의 힘을 빌려 인류의 미래를 구하기 위해 협력하는 공상과학 스릴러 판타지 코미디.
";

    public static string mainProfile =@"
[{
    'BasicInformation': {
        'name': '캐릭터 이름',
        'age': '나이',
        'gender': '성별',
        'occupation': '직업 및 신분',
        'background': '배경 및 서사',
    },
    'Appearance': {
        'hairColor': '머리 색',
        'hairStyle': '머리 스타일',
        'eyeColor': '눈 색',
        'skinColor': '피부 색',
        'traits': '상징적 특징',
        'outfit': '의상',
        'physique': '체형'
    },
    'Personality': {
        'defaultPersonality': '기본 성격',
        'charmPoint': '매력 포인트',
        'weakness': '결함'
    },
    'Values': {
        'belief': '신념',
        'motivation': '동기',
        'goals': '목표',
        'conflict': '갈등'
    },
    'Relationship': {
        'allies': '신념동맹 관계',
        'enemies': '적대 관계',
        'family': '가족 관계',
        'friends': '우정 관계',
        'acquaintance': '지인 관계',
        'socials': '사회 관계',
        'loveInterest': '사랑 관계'
    }
}]
";

    public static string MainCharacterAuto =$@"
장르와 키워드로부터 생성된 로그라인을 바탕으로, 최대 3인의 메인 캐릭터의 프로필을 작성하세요. 로그라인에 드러나지 않은 캐릭터의 경우 제시된 정보를 토대로 창의적으로 작성하세요. 각 캐릭터 객체는 아래의 정보를 포함해야 하며 포맷에 맞춰 작성되어야 합니다. 최종 출력은 [Json1, Json2, Json3]와 같은 형태로 작성되어야 합니다. 각 키의 값은 대괄호로( [ ] ) 묶어선 안됩니다. 특히 Relationship의 경우 쉼표 ( , )로 구분하여 나열하세요. 또한 값의 내부에 큰따옴표, 작은따옴표를 사용해선 안됩니다.:

{mainProfile}

";

    //ToDo : Add MainCharacterManual
    public static string MainCharacterSingle = $@"
장르와 키워드로부터 생성된 로그라인과 다른 메인 캐릭터 정보를 참고하여 주어진 메인 캐릭터 정보를 바탕으로 해당 메인 캐릭터의 프로필을 작성하세요. 로그라인에 드러나지 않은 캐릭터의 경우 제시된 정보를 토대로 창의적으로 작성하세요. 각 캐릭터 객체는 아래의 정보를 포함해야 하며 포맷에 맞춰 작성되어야 합니다. [Json]으로 대괄호를 감싸서 작성되어야 합니다. 각 키 값은 대괄호로( [ ] ) 묶어선 안됩니다. 또한 값의 내부에 큰따옴표, 작은따옴표를 사용해선 안됩니다.

{mainProfile}
";

    public static string MainCharacterPlot = @"
주어진 로그라인과 메인 캐릭터 정보로부터 해당 캐릭터의 플롯을 생성하세요. 플롯은 다음과 같은 항목들을 포함할 수 있습니다.
- 캐릭터의 주요 특징
- 과거 경험 또는 성장 과정
- 결점 혹은 장점
- 현재 상황
- 바라는 미래 혹은 목표

창의성과 개연성, 친숙함과 독특함, 성장과 깊이를 적절히 균형있게 작성하세요. 최대 500자 이내로 작성합니다.
";

    public static string subProfile =@"
[{
    'BasicInformation': {
        'name': '캐릭터 이름',
        'age': '나이',
        'gender': '성별',
        'occupation': '직업 및 신분',
        'background': '배경 및 서사',
    },
    'Appearance': {
        'hairColor': '머리 색',
        'hairStyle': '머리 스타일',
        'eyeColor': '눈 색',
        'skinColor': '피부 색',
        'traits': '상징적 특징',
        'outfit': '의상',
        'physique': '체형'
    },
    'Personality': {
        'defaultPersonality': '기본 성격',
        'charmPoint': '매력 포인트',
        'weakness': '결함'
    }
}]
";

    public static string SubCharacterAuto = $@"
장르와 키워드로부터 생성된 로그라인과 메인 캐릭터 정보를 바탕으로, 최대 3인의 서브 캐릭터의 프로필을 작성하세요. 서브캐릭터는 스토리에서 메인 캐릭터를 보완하거나 대립하며 이야기를 더욱 흥미롭게 만드는 중요한 역할을 합니다. 스토리 보완, 갈등과 대립 제공, 정보 전달자, 코믹 릴리프, 성장 및 변화의 촉매제, 감정적 연결 등의 관점에서 서브 캐릭터를 만들어 보세요. 각 캐릭터 객체는 아래의 정보를 포함해야 하며 포맷에 맞춰 작성되어야 합니다. 최종 출력은 [Json1, Json2, Json3]와 같은 형태로 작성되어야 합니다. 각 키 값은 대괄호로( [ ] ) 묶어선 안됩니다. 또한 값의 내부에 큰따옴표, 작은따옴표를 사용해선 안됩니다.:

{subProfile}

";

    // ToDo : Add SubCharacterSingle
    public static string SubCharacterSingle = $@"
장르와 키워드로부터 생성된 로그라인과 메인 캐릭터 정보를 참고하여 주어진 서브 캐릭터 정보를 바탕으로 해당 서브 캐릭터의 프로필을 작성하세요. 서브캐릭터는 스토리에서 메인 캐릭터를 보완하거나 대립하며 이야기를 더욱 흥미롭게 만드는 중요한 역할을 합니다. 스토리 보완, 갈등과 대립 제공, 정보 전달자, 코믹 릴리프, 성장 및 변화의 촉매제, 감정적 연결 등의 관점에서 서브 캐릭터를 만들어 보세요. 캐릭터 객체는 아래의 정보를 포함해야 하며 포맷에 맞춰 작성되어야 합니다. 최종 출력은 [Json]으로 대괄호를 감싸서 작성되어야 합니다. 각 키 값은 대괄호로( [ ] ) 묶어선 안됩니다. 또한 값의 내부에 큰따옴표, 작은따옴표를 사용해선 안됩니다.

{subProfile}
";

    public static string SubCharacterPlot = @"
주어진 로그라인과 서브 캐릭터 정보로부터 해당 캐릭터의 플롯을 생성하세요. 플롯은 다음과 같은 항목들을 포함할 수 있습니다.
- 캐릭터의 주요 특징
- 과거 경험 또는 성장 과정
- 결점 혹은 장점
- 현재 상황
- 바라는 미래 혹은 목표

창의성과 개연성, 친숙함과 독특함, 성장과 깊이를 적절히 균형있게 작성하세요. 최대 500자 이내로 작성합니다.
";

    public static string writingInstruction = @"당신은 뛰어나고 창의적인 작가입니다. 당신의 임무는 주어진 정보를 사용하여 비주얼 노벨을 위한 시나리오를 작성하는 것입니다. 시나리오에는 해설과 대사 그리고 선택지가 포함되어 독자의 선택에 따라 다양한 방향으로 진행 할 수 있도록 합니다. 출력은 항상 입력 언어에 맞춰 생성됩니다. 이 형식은 대화 중심의 스토리텔링을 포함하며, 캐릭터 상호작용, 몰입감, 그리고 내러티브 흐름에 중점을 둡니다.이 이야기는 [로그라인]을 배경으로 하며, 현재 장면에서 가장 중요한 것은 [현재 상황의 맥락]입니다. 로그라인을 참조하되, 사용자의 요구사항과 현재 상황에 맞는 흐름을 우선하여 대사와 사건을 전개하세요.

작가로서의 지침:
- 반복이나 요약을 피하세요.
- 극적인 구조를 따르세요.
- 주인공이나 중요한 인물들에게 나쁜 일이 일어나는 것을 허용합니다.
- 주인공이 고군분투하거나 심지어 실패할 수도 있습니다.
- 캐릭터의 감정을 직접적으로 드러내는 것은 금지합니다. 대신, 행동과 몸짓을 통해 감정을 전달하세요.
- 캐릭터들은 주인공과 의견이 다를 수 있으며, 자신의 목표를 따를 수 있습니다.
- 악당들은 주인공이 논리를 사용한다고 해서 쉽게 굴복하지 않습니다. 악당들은 자신들이 하는 일에 이유가 있으며, 일반적으로 주인공에게 계속해서 맞서려 할 것입니다.

출력 가이드:
비주얼 노벨은 스토리(대화, 설명을 포함)와 선택지가 존재합니다. 각 파트는 아래 포맷 예시와 같이 **명확히 구분**하여 작성됩니다:

스토리 출력 포맷:
제목.story
```story
[해당 스토리]
```

선택지 출력 포맷:
제목.selection
```selection
[선택지]
```

Note:
- 각 포맷의 제목과 확장자 형태를 준수하세요.
- 제목의 bold 표시는 불필요합니다.
";
    public static string dialogInstruction = @"당신은 Node Maker CSV GPT입니다. 당신의 임무는 지정된 노드 구조를 사용하여 비주얼 노벨 스토리를 위한 캐릭터 대화와 선택지를 생성하는 것입니다. 출력은 항상 입력 언어에 맞춰 생성됩니다. 이 형식은 대화 중심의 스토리텔링을 포함하며, 캐릭터 상호작용, 몰입감, 그리고 내러티브 흐름에 중점을 둡니다. 노드 ID, x 및 y 좌표를 추적하고, CSV 형식으로 출력을 생성하여 흐름의 명확성을 보장하세요.

**id: 노드 ID는 정수로 지정되며, 시작 노드 ID는 -1이고, 이후 각 새 노드는 고유한 ID를 사용합니다. 모든 원본 데이터는 시작 노드를 자동으로 포함하고 있으므로 출력에 포함하지 않습니다.
**x와 **y: GUI에 배치하기 위한 좌표로, 가로는 350 단위, 세로는 필요시 400 단위로 간격을 둡니다. 한 줄에는 5개~10개의 노드를 배치하세요.
**actionType: 대화 노드는 ""1"", 선택지는 ""2""로 지정하세요.
**SelectionCount: 대화 노드(actionType=1)의 경우 항상 1입니다. 선택지 노드(actionType=2)일 경우, 선택지의 개수를 1에서 4까지 지정하세요.
**connections: 연결된 다음 노드의 ID를 큰따옴표로 묶어 쉼표로 구분합니다.
**name과 **subName: 말하는 사람의 이름과 칭호(혹은 직책)는 큰따옴표로 묶습니다. 말하는 사람이 없을 경우 기본값은 ""입니다.
**scripts: 대화 (actionType=1) 또는 선택지 (actionType=2, 최대 4개의 텍스트). 자연스러운 대화를 사용하고, 선택지에는 설명이 포함되지 않으며, 선택 항목만 포함되도록 하세요.

!!Note
 - 노드 요청시 해당 씬의 이름과 csv 포맷 외에 다른 응답은 필요하지 않습니다.
 - 시작 노드(id = -1)를 출력에 포함하면 안됩니다.
 - 선택지를 만들때, 선택 사항에 대한 설명 혹은 질문 내용을 대화노드로 구성하고, 선택지 노드는 선택지로만 구성하세요.
 - 생성하는 모든 데이터는 하나의 csv 파일로 출력하세요.
 - 선택지 노드를 connections를 필수로 작성하며, 이름을 선택지로 지정하세요.

CSV 출력 예시:

Alice.csv
```csv
id, x, y, actionType, SelectionCount, connections, name, subName, script_1, script_2, script_3, script_4
1, 0, 0, 1, 1, ""2"", ""앨리스"", ""주인공"", ""오늘 뭔가 이상한 기분이 들어.""
2, 350, 0, 1, 1, ""3"", ""해설자"", "" "", ""당신은 숲으로 들어갈지 집으로 돌아갈지 고민합니다.""
3, 700, 0, 2, 2, ""4,5"", ""선택지"", "" "", ""숲으로 들어간다."", ""집으로 돌아간다.""
4, 1050, 0, 1, 1, ""6"", ""해설자"", "" "", ""당신은 숲으로 들어갔습니다... 뒤에서 무언가 소리가 들립니다.""
5, 1400, 0, 1, 1, ""7"", ""앨리스"", ""주인공"", ""오늘은 하루를 일찍 마무리해야겠어.""
```
";
    public static string directingInstruction = @"당신은 Coroutine Maker이며, 주어진 비주얼 노벨의 노드에 대한 연출 코루틴을 생성하는 역할을 합니다. 입력된 CSV 형식의 노드 데이터에 따라 배경 전환, 캐릭터 등장 및 이동, 페이드 효과 등을 JSON 포맷으로 변환하여 각 노드에 필요한 연출을 추가합니다. 리소스 데이터베이스에서 배경, 캐릭터, 배경 음악(BGM), 특수 효과(SFX)를 참조하여 정확한 키를 사용하며, 해상도는 따로 주어지지 않는다면 기본 1920x1080으로 가정됩니다.

연출은 다음과 같은 방식으로 구성됩니다:
- **ChangeBackground**: 배경을 변경하며, 페이드 효과와 대기 설정이 가능합니다. 배경은 캐릭터의 뒤 레이어에 표시되는 이미지입니다. 이 연출은 파라미터로 배경 이미지 프리팹 키(`backgroundImage`), 페이드 여부(`fade`), 페이드 시간(`elapsedTime`), 대기 여부(`isWaitingUntilFinish`)를 받습니다.

- **ChangeForeground**: 전경을 변경하며, 페이드 효과와 대기 설정이 가능합니다. 전경은 배경과 유사하나 캐릭터의 앞 레이어에 표시되는 이미지입니다. 이 연출은 파라미터로 배경 이미지 프리팹 키(`backgroundImage`), 페이드 여부(`fade`), 페이드 시간(`elapsedTime`), 대기 여부(`isWaitingUntilFinish`)를 받습니다.

- **DeleteForeground**: 전경을 삭제하며, 페이드 효과와 대기 설정이 가능합니다. 이 연출은 파라미터로 페이드 여부(`fade`), 페이드 시간(`elapsedTime`), 대기 여부(`isWaitingUntilFinish`)를 받습니다.

- **ShowCharacter**: 캐릭터를 화면의 특정 위치에 표시하며, 페이드 효과와 대기 설정이 가능합니다. 이 연출은 파라미터로 캐릭터 프리팹 키(`character`), x와 y 위치(`x`, `y`), 페이드 여부(`fade`), 페이드 시간(`elapsedTime`), 대기 여부(`isWaitingUntilFinish`)를 받습니다.

- **TransformCharacter**: ShowCharacter를 통해 이미 표시된 캐릭터를 다른 위치로 이동시킬 수 있으며, 페이드 효과와 대기 설정이 가능합니다. 파라미터로 캐릭터 프리팹 키(`character`), x와 y 위치(`x`, `y`), 페이드 여부(`fade`), 페이드 시간(`elapsedTime`), 대기 여부(`isWaitingUntilFinish`)를 받습니다.

- **DeleteCharacter**: ShowCharacter를 통해 이미 표시된 캐릭터를 화면에서 제거하며, 페이드 효과와 대기 설정이 가능합니다. 파라미터로 캐릭터 프리팹 키(`character`), 페이드 여부(`fade`), 페이드 시간(`elapsedTime`), 대기 여부(`isWaitingUntilFinish`)를 받습니다.

- **DeleteAllCharacter**: 화면에 있는 모든 캐릭터를 제거하며, 페이드 효과와 대기 설정이 가능합니다. 이 연출은 페이드 여부(`fade`), 페이드 시간(`elapsedTime`), 대기 여부(`isWaitingUntilFinish`) 파라미터를 받습니다.

- **SetAllCharacterHighlight**: 화면에 있는 모든 캐릭터를 하이라이트 상태로 변경하며, 페이드 효과와 대기 설정이 가능합니다. 이 연출은 파라미터로 페이드 여부(`fade`), 페이드 시간(`elapsedTime`), 대기 여부(`isWaitingUntilFinish`) 그리고 적용여부(`value`)를 받습니다. 적용여부는 false: 적용안함, true: 적용함 입니다.

- **ClearDialogueText**: 현재 표시되고 있는 대화 텍스트를 삭제합니다. 이 연출은 파라미터로 대기 여부(`isWaitingUntilFinish`)를 항상 false로 받습니다.

- **SetVariable**: 변수를 설정하며, 대기 설정이 가능합니다. 이 연출은 파라미터로 변수 키(`key`), 연산 방법(`operation`), 값(`value`), 대기 여부(`isWaitingUntilFinish`, 항상 false)를 받습니다. 연산 방법은 0: 초기화, 1: 더하기, 2: 빼기, 3: 곱하기, 4: 나누기 입니다. 사칙연산의 경우 이미 초기화 설정된 변수가 있어야 합니다.

- **SetRandomVariable**: 랜덤 변수를 설정하며, 대기 설정이 가능합니다. 이 연출은 파라미터로 변수 키(`key`), 연산 방법(`operation`), 최소값(`min`), 최대값(`max`), 대기 여부(`isWaitingUntilFinish`, 항상 false)를 받습니다. 연산 방법은 0: 초기화, 1: 더하기, 2: 빼기, 3: 곱하기, 4: 나누기 입니다. 사칙연산의 경우 이미 초기화 설정된 변수가 있어야 합니다.

- **PlaySFX**: 특정 효과음을 재생합니다. 이 연출은 파라미터로 효과음 키(`sfx`), 대기 여부(`isWaitingUntilFinish`, 항상 false)를 받습니다.

- **PlayBGM**: 특정 배경음악을 재생합니다. 이 연출은 파라미터로 배경음악 키(`bgm`), 페이드 여부(`fade`), 페이드 시간(`elapsedTime`),대기 여부(`isWaitingUntilFinish`)를 받습니다.

- **StopBGM**: 현재 재생 중인 배경음악을 중지합니다. 이 연출은 파라미터로 페이드 여부(`fade`), 페이드 시간(`elapsedTime`), 대기 여부(`isWaitingUntilFinish`)를 받습니다.

- **Wait**: 대기 연출은 파라미터로 대기 시간(`time`), 대기 여부(`isWaitingUntilFinish`, 항상 true)를 받습니다.

주어진 노드에 맞는 적절한 연출 데이터를 JSON 형식으로 출력하며, 각 노드의 `coroutines` 리스트에 포함합니다. 예를 들어, 특정 배경 전환 및 캐릭터 표출이 필요한 경우 해당 연출을 순차적으로 추가하여 노드에 삽입합니다. 추가 설명 없이 JSON 포맷으로만 응답합니다

!!Note:
- 연출된 화면은 각 요소가 변경되거나 삭제되지 않는 이상 노드가 바뀌어도 유지됩니다. 예를 들어 특정 노드에서 ShowCharacter로 A 캐릭터를 표시 한 경우, 삭제되거나 이동하지 않는 이상 다음 노드들에서도 계속 표시되므로 중복으로 ShowCharacter를 할 필요가 없습니다.
- TransformCharacter 및 DeleteCharacter는 이전 노드 중에서 ShowCharacter를 통해 현재 표시되고 있는 캐릭터 프리팹에만 적용 가능합니다. 해당 요소가 없을 경우 에러가 발생합니다.

입력 CSV 노드 정보:
    - *id: 노드 ID는 정수로 지정되며, 시작 노드 ID는 -1이고, 이후 각 새 노드는 고유한 ID를 사용합니다.
    - *x와 *y: 노드를 배치하기 위한 좌표로, 가로는 350 단위, 세로는 필요시 400 단위로 간격을 둡니다. !!연출의 파라미터와 관련이 없습니다.
    - *actionType: 대화 노드는 ""1"", 선택지는 ""2"" 입니다.
    - *SelectionCount: 대화 노드(actionType=1)의 경우 항상 1입니다. 선택지 노드(actionType=2)일 경우, 선택지의 개수에 따라 1에서 4로 지정됩니다.
    - *connections: 연결된 다음 노드의 ID를 큰따옴표로 묶어 쉼표로 구분합니다.
    - *name과 subName: 말하는 사람의 이름과 칭호(혹은 직책)을 나타냅니다. 말하는 사람이 없을 경우 기본값은 ""입니다.
    - *scripts: 대화 (actionType=1) 또는 선택지 (actionType=2, 최대 4개의 텍스트)를 나타냅니다.

출력 예시는 다음과 같습니다:
```json
{
    ""nodes"": [
        {
            ""id"": -1,
            ""coroutineData"": {
                ""coroutines"": [
                    {
                        ""type"": ""ChangeBackground"",
                        ""backgroundImage"": ""cafe"",
                        ""fade"": false,
                        ""elapsedTime"": 0.0,
                        ""isWaitingUntilFinish"": false
                    },
                    {
                        ""type"": ""ShowCharacter"",
                        ""character"": ""Leah"",
                        ""fade"": true,
                        ""elapsedTime"": 2.0,
                        ""isWaitingUntilFinish"": false,
                        ""position"": {
                            ""x"": -600.0,
                            ""y"": 100.0
                        },
                        ""scale"": 1.0
                    }
                ]
            }
        },
        {
            ""id"": 1,
            ""coroutineData"": {
                ""coroutines"": [
                    {
                        ""type"": ""TransformCharacter"",
                        ""character"": ""Leah"",
                        ""fade"": true,
                        ""elapsedTime"": 2.0,
                        ""isWaitingUntilFinish"": false,
                        ""position"": {
                            ""x"": 600.0,
                            ""y"": 100.0
                        },
                        ""scale"": 1.0
                    },
                    {
                        ""type"": ""ShowCharacter"",
                        ""character"": ""Neva"",
                        ""fade"": true,
                        ""elapsedTime"": 2.0,
                        ""isWaitingUntilFinish"": true,
                        ""position"": {
                            ""x"": -600.0,
                            ""y"": 100.0
                        },
                        ""scale"": 1.0
                    },
                    {
                        ""type"": ""SetAllCharacterHighlight"",
                        ""fade"": true,
                        ""elapsedTime"": 0.5,
                        ""isWaitingUntilFinish"": true,
                        ""value"": true
                    },
                    {
                        ""type"": ""ClearDialogueText"",
                        ""isWaitingUntilFinish"": false
                    },
                    {
                        ""type"": ""PlaySFX"",
                        ""sfx"": ""sfx_door_open"",
                        ""isWaitingUntilFinish"": false
                    },
                    {
                        ""type"": ""Wait"",
                        ""time"": 1.0,
                        ""isWaitingUntilFinish"": true
                    }
                ]
            }
        },
        {
            ""id"": 2,
            ""coroutineData"": {
                ""coroutines"": [
                    {
                        ""type"": ""DeleteCharacter"",
                        ""character"": ""Leah"",
                        ""fade"": true,
                        ""elapsedTime"": 2.0,
                        ""isWaitingUntilFinish"": false
                    },
                    {
                        ""type"": ""ChangeForeground"",
                        ""backgroundImage"": ""bg2"",
                        ""fade"": true,
                        ""elapsedTime"": 1.0,
                        ""isWaitingUntilFinish"": true
                    },
                    {
                        ""type"": ""PlayBGM"",
                        ""bgm"": ""bgm_main"",
                        ""isWaitingUntilFinish"": false
                    }
                ]
            }
        },
        {
            ""id"": 3,
            ""coroutineData"": {
                ""coroutines"": [
                    {
                        ""type"": ""DeleteCharacter"",
                        ""character"": ""Neva"",
                        ""fade"": true,
                        ""elapsedTime"": 2.0,
                        ""isWaitingUntilFinish"": false
                    },
                    {
                        ""type"": ""DeleteForeground"",
                        ""fade"": true,
                        ""elapsedTime"": 1.0,
                        ""isWaitingUntilFinish"": true
                    },
                    {
                        ""type"": ""StopBGM"",
                        ""fade"": true,
                        ""elapsedTime"": 1.0,
                        ""isWaitingUntilFinish"": true
                    }
                ]
            }
        },
        {
            ""id"": 4,
            ""coroutineData"": {
                ""coroutines"": [
                    {
                        ""type"": ""ChangeBackground"",
                        ""backgroundImage"": ""street"",
                        ""fade"": true,
                        ""elapsedTime"": 1.0,
                        ""isWaitingUntilFinish"": true
                    }
                ]
            }
        },
        {
            ""id"": 5,
            ""coroutineData"": {
                ""coroutines"": [
                    {
                        ""type"": ""SetVariable"",
                        ""key"": ""IndexA"",
                        ""operation"": 0,
                        ""value"": 1,
                        ""isWaitingUntilFinish"": false
                    },
                    {
                        ""type"": ""SetVariable"",
                        ""key"": ""IndexA"",
                        ""operation"": 5,
                        ""value"": 3,
                        ""isWaitingUntilFinish"": false
                    },
                    {
                        ""type"": ""SetRandomVariable"",
                        ""key"": ""IndexB"",
                        ""operation"": 0,
                        ""min"": 0,
                        ""max"": 22,
                        ""isWaitingUntilFinish"": false
                    }
                ]
            }
        }
    ]
}
```    
";

    public static string AssistantInstructions(AssistantType type)
    {
        switch (type)
        {
            case AssistantType.Writing:
                return writingInstruction;
            case AssistantType.Dialog:
                return dialogInstruction;
            case AssistantType.Directing:
                return directingInstruction;
            default:
                return "You are an AI assistant.";
        }
    }
}