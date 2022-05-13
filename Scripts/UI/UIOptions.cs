using Godot;
using System;

namespace GodotModules
{
    public class UIOptions : Node
    {
        [Export] public readonly NodePath NodePathUIControls;
        [Export] public readonly NodePath NodePathOptionsGame;
        [Export] public readonly NodePath NodePathOptionsVideo;
        [Export] public readonly NodePath NodePathOptionsAudio;
        [Export] public readonly NodePath NodePathOptionsControls;
        [Export] public readonly NodePath NodePathOptionsMultiplayer;

        private UIControls _uiControls;
        private Dictionary<OptionSection, Control> _optionSections;

        public void PreInit(HotkeyManager hotkeyManager)
        {
            _uiControls = GetNode<UIControls>(NodePathUIControls);
            _uiControls._hotkeyManager = hotkeyManager;
        }

        public override void _Ready()
        {
            _optionSections = new();
            _optionSections[OptionSection.Game] = GetNode<Control>(NodePathOptionsGame);
            _optionSections[OptionSection.Video] = GetNode<Control>(NodePathOptionsVideo);
            _optionSections[OptionSection.Audio] = GetNode<Control>(NodePathOptionsAudio);
            _optionSections[OptionSection.Controls] = GetNode<Control>(NodePathOptionsControls);
            _optionSections[OptionSection.Multiplayer] = GetNode<Control>(NodePathOptionsMultiplayer);
            ShowSection(OptionSection.Game);
        }

        private void _on_Game_pressed() => ShowSection(OptionSection.Game);
        private void _on_Video_pressed() => ShowSection(OptionSection.Video);
        private void _on_Audio_pressed() => ShowSection(OptionSection.Audio);
        private void _on_Controls_pressed() => ShowSection(OptionSection.Controls);
        private void _on_Multiplayer_pressed() => ShowSection(OptionSection.Multiplayer);

        private void ShowSection(OptionSection section)
        {
            void HideAllSections() => _optionSections.ForEach(x => x.Value.Hide());

            HideAllSections();
            _optionSections[section].Visible = true;
        }

        private enum OptionSection 
        {
            Game,
            Video,
            Audio,
            Controls,
            Multiplayer
        }
    }
}
