using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using SysTrace = System.Diagnostics.Trace; // Autodesk.AutoCAD.DatabaseServices.Trace(도형 엔티티)와 이름 충돌 방지

namespace JH.SmokeTest;

// =====================================================================
// 1. 기본 CommandMethod 테스트
//    - CadAddinManager Manual 창에서 더블클릭 실행
//    - 코드 수정 후엔 Faceless로 재실행
// =====================================================================
public class HelloCommand
{
    // 코드를 고칠 때마다 이 문자열만 바꿔서 재빌드 -> Faceless 재실행 시
    // 바뀐 문자열이 뜨면 "재시작 없이 새 코드가 반영된다"는 게 확실히 증명됩니다.
    private const string BuildMark = "v3";

    [CommandMethod("JH_TEST_HELLO")]
    public void Execute()
    {
        var ed = Application.DocumentManager.MdiActiveDocument.Editor;
        ed.WriteMessage($"\nHello from JH SmokeTest! (build mark: {BuildMark})\n");
    }
}

// =====================================================================
// 2. Trace/Debug 패널 확인용
//    - Civil3D 리본의 Dockpanel(Trace/Debug 창)에 아래 5줄이 색상별로 뜨는지 확인
//    - 매뉴얼 예시 그대로
// =====================================================================
public class DebugTraceTest
{
    [CommandMethod("JH_TEST_TRACE")]
    public void Action()
    {
        SysTrace.WriteLine("Warning: This is a warning");
        SysTrace.WriteLine("Error: This is a error");
        SysTrace.WriteLine("Add: This is a add");
        SysTrace.WriteLine("Modify: This is a modify");
        SysTrace.WriteLine("Delete: This is a delete");
    }
}

// =====================================================================
// 3. LispFunction 테스트
//    - 실행: 명령줄에 (jh_test_fullname "Hong" "Gildong") 입력
// =====================================================================
public class LispFunctionTest
{
    [LispFunction("jh_test_fullname")]
    public static void DisplayFullName(ResultBuffer rbArgs)
    {
        if (rbArgs == null) return;

        string first = string.Empty;
        string last = string.Empty;
        int idx = 0;

        foreach (TypedValue rb in rbArgs)
        {
            if (rb.TypeCode == (int)LispDataType.Text)
            {
                if (idx == 0) first = rb.Value.ToString() ?? string.Empty;
                else if (idx == 1) last = rb.Value.ToString() ?? string.Empty;
                idx++;
            }
        }

        var ed = Application.DocumentManager.MdiActiveDocument.Editor;
        ed.WriteMessage($"\nName: {first} {last}\n");
        MessageBox.Show($"Name: {first} {last}", "JH SmokeTest");
    }
}

// =====================================================================
// 4. 예외 케이스 확인용 (agent_result.json의 exception 필드 검증에도 재사용 가능)
//    - 일부러 NullReferenceException을 던져서, Trace/Debug 패널에
//      스택트레이스가 제대로 찍히는지 + CadAddinManager가 죽지 않고
//      다음 명령을 계속 실행할 수 있는지 확인
// =====================================================================
public class FailureCommandTest
{
    [CommandMethod("JH_TEST_FAIL")]
    public void Execute()
    {
        string? nullString = null;
        SysTrace.WriteLine("[JH_TEST_FAIL] 일부러 예외를 발생시킵니다...");
        _ = nullString!.Length; // NullReferenceException 유발
    }
}