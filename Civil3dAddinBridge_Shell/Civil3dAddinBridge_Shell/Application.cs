using Autodesk.Windows;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

using System;
using System.IO;
using System.Linq;
using System.Reflection;

using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;

namespace Civil3dAddinBridge_Shell;

public class Application : IExtensionApplication
{
    // 실제 Civil3dAddinBridge 프로젝트의 빌드 출력 dll 파일명으로 맞춰주세요.
    private const string CoreAssemblyFileName = "JH_CadAddinManager_SmokeTest.dll";
    private const string TabId = "Civil3dAddinBridgeTab";

    public void Initialize()
    {
        LoadCoreAssembly();
        acadApp.Idle += OnIdleInitialize;
    }

    public void Terminate()
    {
        acadApp.Idle -= OnIdleInitialize;
        ComponentManager.ItemInitialized -= OnItemInitialized;
    }

    // ====================================================================
    // Core.dll 로드
    // ====================================================================
    // Civil3dAddinBridge.dll을 이렇게 로드해주면, AutoCAD가 자동으로 그 안의
    // [CommandMethod]들을 전역 커맨드 테이블에 등록한다. 그러면 리본 버튼에서는
    // 타입을 몰라도 SendStringToExecute로 커맨드 이름만 부르면 실행된다.
    // ====================================================================
    private static void LoadCoreAssembly()
    {
        try
        {
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string corePath = Path.Combine(baseDir, CoreAssemblyFileName);

            bool alreadyLoaded = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => string.Equals(
                    a.GetName().Name,
                    Path.GetFileNameWithoutExtension(CoreAssemblyFileName),
                    StringComparison.OrdinalIgnoreCase));

            if (alreadyLoaded) return;

            if (File.Exists(corePath))
            {
                Assembly.LoadFrom(corePath);
            }
            else
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[Civil3dAddinBridge.Shell] 경고: Core dll을 찾을 수 없습니다: {corePath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(
                $"[Civil3dAddinBridge.Shell] Core dll 로드 실패: {ex.Message}");
        }
    }

    // ====================================================================
    // 리본 초기화 타이밍
    // ====================================================================
    private void OnIdleInitialize(object? sender, EventArgs e)
    {
        acadApp.Idle -= OnIdleInitialize;
        EnsureRibbonTab();
    }

    private void EnsureRibbonTab()
    {
        if (ComponentManager.Ribbon != null)
        {
            AddRibbonTab();
        }
        else
        {
            ComponentManager.ItemInitialized -= OnItemInitialized;
            ComponentManager.ItemInitialized += OnItemInitialized;
        }
    }

    private void OnItemInitialized(object? sender, EventArgs e)
    {
        ComponentManager.ItemInitialized -= OnItemInitialized;
        if (ComponentManager.Ribbon != null)
            EnsureRibbonTab();
    }

    // ====================================================================
    // 리본 빌드 (재로드 안전: 이미 있으면 다시 안 만듦)
    // ====================================================================
    private void AddRibbonTab()
    {
        var rc = ComponentManager.Ribbon;
        if (rc == null) return;

        var existing = rc.Tabs.FirstOrDefault(t =>
            string.Equals(t.Id, TabId, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.IsVisible = true;
            return;
        }

        var tab = new RibbonTab { Title = "Civil3dAddinBridge", Id = TabId };
        rc.Tabs.Add(tab);

        var panelSource = new RibbonPanelSource { Title = "Test" };
        var panel = new RibbonPanel { Source = panelSource };
        tab.Panels.Add(panel);

        panelSource.Items.Add(MakeButton("Hello", "JH_TEST_HELLO"));
        panelSource.Items.Add(MakeButton("Trace", "JH_TEST_TRACE"));
        panelSource.Items.Add(MakeButton("Fail 테스트", "JH_TEST_FAIL"));
    }

    private static RibbonButton MakeButton(string text, string commandName)
    {
        return new RibbonButton
        {
            Text = text,
            ShowText = true,
            Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            CommandHandler = new RunCommandHandler(commandName)
        };
    }

    // ====================================================================
    // 커맨드 실행기: SendStringToExecute로 이름만 불러서 실행
    // (Core.dll의 클래스/네임스페이스를 전혀 몰라도 됨 - 커맨드 이름 문자열만 필요)
    // ====================================================================
    public class RunCommandHandler : System.Windows.Input.ICommand
    {
        private readonly string _commandName;

        public RunCommandHandler(string commandName) => _commandName = commandName;

#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            var doc = acadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                acadApp.ShowAlertDialog("활성 문서가 없습니다.");
                return;
            }

            // 끝에 공백을 반드시 붙여야 명령이 실행됨 (AutoCAD 명령줄 규칙)
            doc.SendStringToExecute($"{_commandName} ", true, false, false);
        }
    }
}