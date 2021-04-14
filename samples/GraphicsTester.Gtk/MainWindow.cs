// This is free and unencumbered software released into the public domain.
// Happy coding!!! - GtkSharp Team

using Gtk;
using System;
using System.Collections.Generic;
using System.IO;
using GraphicsTester.Scenarios;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

namespace Samples {

    class MainWindow : Window {

        private HeaderBar _headerBar;
        private TreeView _treeView;
        private Box _boxContent;
        private TreeStore _store;
        private Dictionary<string, AbstractScenario> _items;
        private Notebook _notebook;
        private GtkSkiaGraphicsView _skiaGraphicsView;
        private GtkSkiaDirectRenderer _skiaGraphicsRenderer;

        public MainWindow () : base (WindowType.Toplevel) {
            // Setup GUI
            WindowPosition = WindowPosition.Center;
            DefaultSize = new Gdk.Size (800, 600);

            _headerBar = new HeaderBar ();
            _headerBar.ShowCloseButton = true;
            _headerBar.Title = "GtkSharp Sample Application";

            var btnClickMe = new Button ();
            btnClickMe.AlwaysShowImage = true;
            btnClickMe.Image = Image.NewFromIconName ("document-new-symbolic", IconSize.Button);
            _headerBar.PackStart (btnClickMe);

            Titlebar = _headerBar;

            var hpanned = new HPaned ();
            hpanned.Position = 200;

            _treeView = new TreeView ();
            _treeView.HeadersVisible = false;
            hpanned.Pack1 (_treeView, false, true);

            Fonts.Register (new SkiaFontService ("", ""));
            GraphicsPlatform.RegisterGlobalService (SkiaGraphicsService.Instance);

            _skiaGraphicsView = new GtkSkiaGraphicsView {
                BackgroundColor = Colors.White
            };

            _skiaGraphicsRenderer = new GtkSkiaDirectRenderer {
                BackgroundColor = Colors.White
            };
            _skiaGraphicsView.Renderer = _skiaGraphicsRenderer;

            var scroll1 = new ScrolledWindow ();
            scroll1.Child = _skiaGraphicsView;

            hpanned.Pack2 (scroll1, true, true);

            Child = hpanned;

            // Fill up data
            FillUpTreeView ();

            // Connect events
            _treeView.Selection.Changed += Selection_Changed;
            Destroyed += (sender, e) => Application.Quit ();

            var scenario = ScenarioList.Scenarios[0];
            _skiaGraphicsView.Drawable = scenario;
        }

        private void Selection_Changed (object sender, EventArgs e) {
            if (_treeView.Selection.GetSelected (out TreeIter iter)) {
                var s = _store.GetValue (iter, 0).ToString ();

                if (_items.TryGetValue (s, out var scenario)) {
                    _skiaGraphicsView.Drawable = scenario;
                }

            }
        }

        private void FillUpTreeView () {
            // Init cells
            var cellName = new CellRendererText ();

            // Init columns
            var columeSections = new TreeViewColumn ();
            columeSections.Title = "Sections";
            columeSections.PackStart (cellName, true);

            columeSections.AddAttribute (cellName, "text", 0);

            _treeView.AppendColumn (columeSections);

            // Init treeview
            _store = new TreeStore (typeof(string));
            _treeView.Model = _store;
            _items = new Dictionary<string, AbstractScenario> ();

            foreach (var scenario in ScenarioList.Scenarios) {

                _store.AppendValues (scenario.ToString ());
                _items[scenario.ToString ()] = scenario;

            }

            _treeView.ExpandAll ();
        }

    }

}