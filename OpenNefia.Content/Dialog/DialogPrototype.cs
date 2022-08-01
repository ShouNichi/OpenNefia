﻿using OpenNefia.Core;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Serialization.Manager.Attributes;
using OpenNefia.Core.UI;
using OpenNefia.Core.UserInterface;

namespace OpenNefia.Content.Dialog
{
    [Prototype("Elona.Dialog")]
    public class DialogPrototype : IPrototype
    {
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField(required: true)]
        public string StartNode { get; } = "__start__";

        [DataField("nodes", required: true)]
        private Dictionary<string, IDialogNode> _nodes = new();

        public IReadOnlyDictionary<string, IDialogNode> Nodes => _nodes;
    }

    public delegate void DialogNodeDelegate(IDialogEngine engine, IDialogNode node);

    [ImplicitDataDefinitionForInheritors]
    public interface IDialogNode
    {
        string ID { get; }

        string? Invoke(IDialogEngine engine);
        string? GetDefaultNode(IDialogEngine engine);
    }

    [DataDefinition]
    public sealed class TextNodeChoice
    {
        [DataField]
        public string? NextNode { get; set; }

        [DataField]
        public LocaleKey Text { get; set; }
    }

    public sealed class DialogTextNode : IDialogNode
    {
        /// <inheritdoc/>
        [DataField(required: true)]
        public string ID { get; } = default!;

        [DataField("texts", required: true)]
        private List<LocaleKey> _texts { get; } = new();

        public IReadOnlyList<LocaleKey> Texts => _texts;

        [DataField("choices", required: true)]
        private List<TextNodeChoice> _choices { get; } = new();

        public IReadOnlyList<TextNodeChoice> Choices => _choices;

        public string? Invoke(IDialogEngine engine)
        {
            if (_texts.Count == 0)
                return null;

            var uiMan = IoCManager.Resolve<IUserInterfaceManager>();
            var entityMan = IoCManager.Resolve<IEntityManager>();
            var dialog = EntitySystem.Get<IDialogSystem>();

            UiResult<DialogResult>? result = null;
            
            for (var i = 0; i < _texts.Count; i++)
            {
                var text = _texts[i];

                List<DialogChoice> choices = new();
                if (i == _texts.Count - 1)
                {
                    choices.Add(new() { Text = Loc.GetString("Elona.Dialog.Common.Choices.Bye") });
                }
                else
                {
                    choices.Add(new() { Text = Loc.GetString("Elona.Dialog.Common.Choices.More") });
                }

                var speakerName = "";
                if (entityMan.IsAlive(engine.Target))
                    speakerName = dialog.GetDefaultSpeakerName(engine.Target.Value);

                var step = new DialogStepData()
                {
                    Target = engine.Target,
                    SpeakerName = speakerName,
                    Text = Loc.GetString(text),
                    Choices = choices
                };

                engine.DialogLayer.UpdateFromStepData(step);
                result = uiMan.Query(engine.DialogLayer);
            }

            if (result == null)
                return null;

            int defaultChoiceIndex = 0; // TODO
            int choiceIndex = 0;
            if (result is UiResult<DialogResult>.Cancelled)
            {
                choiceIndex = defaultChoiceIndex;
            }
            else if (result is UiResult<DialogResult>.Finished resultFinished)
            {
                choiceIndex = resultFinished.Value.SelectedChoiceIndex;
            }

            var nextNodeID = _choices.ElementAtOrDefault(choiceIndex)?.NextNode;
            if (nextNodeID == null)
            {
                return null;
            }

            return nextNodeID;
        }

        public string? GetDefaultNode(IDialogEngine engine)
        {
            int defaultChoiceIndex = 0; // TODO

            var nextNodeID = _choices.ElementAtOrDefault(defaultChoiceIndex)?.NextNode;
            if (nextNodeID == null)
            {
                return null;
            }

            return nextNodeID;
        }
    }

    public sealed class DialogCallbackNode : IDialogNode
    {
        /// <inheritdoc/>
        [DataField(required: true)]
        public string ID { get; } = default!;

        [DataField(required: true)]
        DialogNodeDelegate Callback { get; } = default!;

        [DataField(required: true)]
        public string NextNode { get; } = default!;

        public string? Invoke(IDialogEngine engine)
        {
            Callback(engine, this);
            return NextNode;
        }

        public string? GetDefaultNode(IDialogEngine engine) => NextNode;
    }
}
