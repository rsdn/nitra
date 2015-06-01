﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using Nitra.Visualizer.Annotations;

using System.IO;
using System.Linq;
using Nitra.Declarations;
using Nitra.ProjectSystem;

namespace Nitra.ViewModels
{
  public class TestFolderVm : FullPathVm, ITest
  {
    public string                       TestPath          { get; private set; }
    public TestSuitVm                   TestSuit          { get; private set; }
    public string                       Name              { get { return Path.GetFileNameWithoutExtension(TestPath); } }
    public ObservableCollection<TestVm> Tests             { get; private set; }
    //public ObservableCollection<IAst>   CompilationUnits  { get; private set; }

    public TestFolderVm(string testPath, TestSuitVm testSuit, ICompilerMessages compilerMessages)
      : base(testSuit, testPath)
    {
      var solution = new FsSolution<IAst>();
      this.Project = new FsProject<IAst>(compilerMessages, solution);

      Statistics            = new StatisticsTask.Container("Total");
      ParsingStatistics     = Statistics.ReplaceContainerSubtask("Parsing");
      AstStatistics         = Statistics.ReplaceContainerSubtask("Ast", "AST Creation");
      DependPropsStatistics = Statistics.ReplaceContainerSubtask("DependProps", "Dependent properties");

      TestPath = testPath;
      TestSuit = testSuit;
      if (TestSuit.TestState == TestState.Ignored)
        TestState = TestState.Ignored;

      string testSuitPath = base.FullPath;
      var tests = new ObservableCollection<TestVm>();

      var paths = Directory.GetFiles(testSuitPath, "*.test");
      foreach (var path in paths.OrderBy(f => f))
        tests.Add(new TestVm(path, TestSuit, compilerMessages));

      Tests = tests;
    }

    public override string Hint { get { return "TestFolder"; } }

    public void Update([NotNull] string code, [NotNull] string gold)
    {
    }

    public void Remove()
    {
    }

    public override string ToString()
    {
      return Name;
    }

    public StatisticsTask.Container Statistics            { get; private set; }
    public StatisticsTask.Container ParsingStatistics     { get; private set; }
    public StatisticsTask.Container AstStatistics         { get; private set; }
    public StatisticsTask.Container DependPropsStatistics { get; private set; }

    public FsProject<IAst> Project
    {
      get; private set; }

    public void CalcDependProps(TestVm testVm)
    {
      
    }
  }
}
