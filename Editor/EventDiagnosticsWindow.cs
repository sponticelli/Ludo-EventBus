using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Ludo.Core.EventBus.Diagnostics;

namespace Ludo.Core.EventBus
{
    public class EventDiagnosticsWindow : EditorWindow
    {
        private enum Tab
        {
            Overview,
            EventFlow,
            Performance,
            Monitoring
        }

        private Tab _currentTab = Tab.Overview;
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _lastRefreshTime;
        private const float RefreshInterval = 1.0f; // Refresh every second
        private string _selectedEventType;
        private List<EventTrace> _cachedTraces = new List<EventTrace>();
        private List<EventAnalysis> _cachedSlowSubscribers = new List<EventAnalysis>();
        private List<EventTypeAnalysis> _cachedFrequencies = new List<EventTypeAnalysis>();

        [MenuItem("Window/Ludo/Analysis/Event Diagnostics")]
        public static void ShowWindow()
        {
            var window = GetWindow<EventDiagnosticsWindow>();
            window.titleContent = new GUIContent("Event Diagnostics");
            window.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_autoRefresh && EditorApplication.timeSinceStartup > _lastRefreshTime + RefreshInterval)
            {
                RefreshData();
                _lastRefreshTime = (float)EditorApplication.timeSinceStartup;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_currentTab)
            {
                case Tab.Overview:
                    DrawOverviewTab();
                    break;
                case Tab.EventFlow:
                    DrawEventFlowTab();
                    break;
                case Tab.Performance:
                    DrawPerformanceTab();
                    break;
                case Tab.Monitoring:
                    DrawMonitoringTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshData();
            }

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);

            if (GUILayout.Button("Clear Data", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Clear Diagnostics",
                        "Are you sure you want to clear all diagnostic data?", "Yes", "No"))
                {
                    EventDiagnostics.ClearDiagnostics();
                    RefreshData();
                }
            }

            GUILayout.FlexibleSpace();

            _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab,
                Enum.GetNames(typeof(Tab)), EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawOverviewTab()
        {
            EditorGUILayout.LabelField("Event System Overview", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // Event Type Statistics
            EditorGUILayout.LabelField("Event Types", EditorStyles.boldLabel);
            foreach (var freq in _cachedFrequencies)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(freq.EventType.Name);
                EditorGUILayout.LabelField(freq.Frequency.ToString(), GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Recent Events Summary
            EditorGUILayout.LabelField("Recent Events", EditorStyles.boldLabel);
            foreach (var trace in _cachedTraces.Take(5))
            {
                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                EditorGUILayout.LabelField(trace.EventType.Name);
                EditorGUILayout.LabelField($"{trace.TotalDurationMs}ms", GUILayout.Width(100));
                EditorGUILayout.LabelField(trace.WasCanceled ? "Canceled" : "Completed", GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawEventFlowTab()
        {
            EditorGUILayout.LabelField("Event Flow Analysis", EditorStyles.boldLabel);

            // Event Type Filter
            var eventTypes = _cachedTraces
                .Select(t => t.EventType.Name)
                .Distinct()
                .ToList();

            int selectedIndex = eventTypes.IndexOf(_selectedEventType);
            selectedIndex = EditorGUILayout.Popup("Event Type", selectedIndex, eventTypes.ToArray());
            if (selectedIndex >= 0 && selectedIndex < eventTypes.Count)
            {
                _selectedEventType = eventTypes[selectedIndex];
            }

            EditorGUILayout.Space();

            // Show detailed flow for selected event type
            var selectedTraces = _cachedTraces
                .Where(t => t.EventType.Name == _selectedEventType)
                .ToList();

            foreach (var trace in selectedTraces)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.LabelField($"Event ID: {trace.EventId}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Timestamp: {trace.Timestamp}");
                EditorGUILayout.LabelField($"Total Duration: {trace.TotalDurationMs}ms");

                // Publisher information
                if (!string.IsNullOrEmpty(trace.PublisherStackTrace))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Publisher Stack Trace:", EditorStyles.boldLabel);

                    // Parse the first line of the stack trace to get the immediate caller
                    string[] stackLines = trace.PublisherStackTrace.Split('\n');
                    if (stackLines.Length > 0)
                    {
                        string publisherInfo = stackLines[^1].Trim();
                        EditorGUILayout.LabelField(publisherInfo, EditorStyles.wordWrappedLabel);

                        // Add a foldout for full stack trace
                        if (GUILayout.Button("Show Full Stack Trace", EditorStyles.miniButton))
                        {
                            EditorGUIUtility.systemCopyBuffer = trace.PublisherStackTrace;
                            EditorUtility.DisplayDialog("Stack Trace",
                                "Full stack trace has been copied to clipboard.", "OK");
                        }
                    }
                }

                if (trace.WasCanceled)
                {
                    EditorGUILayout.LabelField("Status: Canceled", EditorStyles.boldLabel);
                }

                EditorGUILayout.Space();

                // Subscriber chain
                EditorGUILayout.LabelField("Subscribers:", EditorStyles.boldLabel);
                foreach (var invocation in trace.Invocations)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"→ {invocation.SubscriberName}");
                    EditorGUILayout.LabelField($"{invocation.Priority}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"{invocation.DurationMs}ms", GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();

                    if (invocation.Error != null)
                    {
                        EditorGUILayout.HelpBox(invocation.Error.Message, MessageType.Error);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawPerformanceTab()
        {
            EditorGUILayout.LabelField("Performance Analysis", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // Slow Subscribers
            EditorGUILayout.LabelField("Slow Subscribers (>16ms)", EditorStyles.boldLabel);
            foreach (var subscriber in _cachedSlowSubscribers)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(subscriber.Name, EditorStyles.boldLabel);

                // Performance metrics
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Average Duration:", GUILayout.Width(120));
                EditorGUILayout.LabelField($"{subscriber.AverageDurationMs:F2}ms");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Max Duration:", GUILayout.Width(120));
                EditorGUILayout.LabelField($"{subscriber.MaxDurationMs}ms");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Invocation Count:", GUILayout.Width(120));
                EditorGUILayout.LabelField(subscriber.InvocationCount.ToString());
                EditorGUILayout.EndHorizontal();

                // Visual performance bar
                Rect r = EditorGUILayout.GetControlRect(false, 20);
                float width = Mathf.Min((float)(subscriber.AverageDurationMs / 32f), 1f) * r.width;
                EditorGUI.DrawRect(new Rect(r.x, r.y, width, r.height),
                    subscriber.AverageDurationMs > 32f ? Color.red : Color.yellow);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawMonitoringTab()
        {
            EditorGUILayout.LabelField("Real-time Monitoring", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // Event Rate
            float eventsPerSecond = _cachedTraces
                .Count(t => (DateTime.UtcNow - t.Timestamp).TotalSeconds <= 1);

            EditorGUILayout.LabelField($"Events/second: {eventsPerSecond}");

            // Active Events
            var activeEvents = _cachedTraces
                .GroupBy(t => t.EventType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Active Event Types", EditorStyles.boldLabel);

            foreach (var evt in activeEvents)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(evt.Type.Name);
                EditorGUILayout.LabelField(evt.Count.ToString(), GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }

            // Error Detection
            var recentErrors = _cachedTraces
                .SelectMany(t => t.Invocations)
                .Where(i => i.Error != null)
                .Take(5);

            if (recentErrors.Any())
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Recent Errors", EditorStyles.boldLabel);

                foreach (var error in recentErrors)
                {
                    EditorGUILayout.HelpBox(
                        $"{error.SubscriberName}: {error.Error.Message}",
                        MessageType.Error);
                }
            }

            Repaint();
        }

        private void RefreshData()
        {
            _cachedTraces = EventDiagnostics.GetRecentTraces().ToList();
            _cachedSlowSubscribers = EventDiagnostics.GetSlowSubscribers().ToList();
            _cachedFrequencies = EventDiagnostics.GetEventFrequency().ToList();
        }
    }
}