using System;
using System.Linq;
using Pleno.Core.Rng;
using UnityEditor;
using UnityEngine;

namespace Pleno.Sim.Editor
{
    /// <summary>Janela do simulador: escolhe bots, runs e seed mestra, roda em segundo plano e mostra a tabela.</summary>
    public sealed class SimWindow : EditorWindow
    {
        int _runs = 800;
        string _seed = SimConfig.DefaultMasterSeed;
        int _threads = Environment.ProcessorCount;
        bool[] _bots = Bot.All.Select(_ => true).ToArray();
        Vector2 _scroll;

        [MenuItem("Pleno/Simulador/Janela")]
        public static void Open() => GetWindow<SimWindow>("Simulador").Show();

        void OnEnable() => EditorApplication.update += RepaintWhileRunning;
        void OnDisable() => EditorApplication.update -= RepaintWhileRunning;

        void RepaintWhileRunning()
        {
            if (SimJob.Running)
                Repaint();
        }

        void OnGUI()
        {
            using (new EditorGUI.DisabledScope(SimJob.Running))
            {
                _runs = Mathf.Max(1, EditorGUILayout.IntField("Runs por bot", _runs));
                _seed = RunSeed.Normalize(EditorGUILayout.TextField("Seed mestra", _seed));
                _threads = EditorGUILayout.IntSlider("Threads", _threads, 1, Environment.ProcessorCount);
                EditorGUILayout.LabelField("Bots");
                for (int i = 0; i < _bots.Length; i++)
                    _bots[i] = EditorGUILayout.ToggleLeft(Bot.IdOf(Bot.All[i]), _bots[i]);
                EditorGUILayout.LabelField("Roda", "Europeia");
            }

            bool validSeed = RunSeed.IsValid(_seed);
            if (!validSeed)
                EditorGUILayout.HelpBox($"A seed tem {RunSeed.Length} caracteres de {RunSeed.Alphabet}.", MessageType.Warning);

            EditorGUILayout.Space();
            if (SimJob.Running)
            {
                SimProgress p = SimJob.Progress;
                Rect bar = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(bar, p.Total == 0 ? 0 : (float)p.Done / p.Total, $"{p.CurrentBot}: {p.Done}/{p.Total}");
                if (GUILayout.Button("Cancelar"))
                    SimJob.Cancel();
            }
            else
            {
                using (new EditorGUI.DisabledScope(!validSeed || !_bots.Any(b => b)))
                {
                    if (GUILayout.Button("Rodar"))
                    {
                        SimJob.Start(new SimConfig
                        {
                            Runs = _runs,
                            MasterSeed = _seed,
                            Threads = _threads,
                            Bots = Bot.All.Where((_, i) => _bots[i]).ToList(),
                        });
                    }
                }
                if (SimJob.OutputDirectory != null && GUILayout.Button("Abrir pasta dos CSVs"))
                    EditorUtility.RevealInFinder(SimJob.OutputDirectory);
            }

            if (!string.IsNullOrEmpty(SimJob.LastSummary))
            {
                EditorGUILayout.Space();
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                var style = new GUIStyle(EditorStyles.textArea) { font = EditorStyles.miniFont, wordWrap = false };
                EditorGUILayout.TextArea(SimJob.LastSummary, style);
                EditorGUILayout.EndScrollView();
            }
        }
    }
}
