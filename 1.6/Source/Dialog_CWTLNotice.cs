using UnityEngine;
using Verse;

namespace ChooseWhereToLand
{
    public class Dialog_CWTLNotice : Dialog_MessageBox
    {
        public override Vector2 InitialSize => new Vector2(720f, 720f);
        private Vector2 scrollPosition = Vector2.zero;
        private bool InteractionDelayExpired => TimeUntilInteractive <= 0f;
        private float TimeUntilInteractive => interactionDelay - (Time.realtimeSinceStartup - creationRealTime);
        private float creationRealTime = -1f;
        public  Texture2D background = ContentFinder<Texture2D>.Get("NotificationBackground");
        public Dialog_CWTLNotice(TaggedString text, string buttonAText = null, Action buttonAAction = null, string buttonBText = null, Action buttonBAction = null, string title = null, bool buttonADestructive = false, Action acceptAction = null, Action cancelAction = null, WindowLayer layer = WindowLayer.Dialog) : base(text)
        {
            this.text = text;
            this.buttonAText = buttonAText;
            this.buttonAAction = buttonAAction;
            this.buttonADestructive = buttonADestructive;
            this.buttonBText = buttonBText;
            this.buttonBAction = buttonBAction;
            this.title = title;
            this.acceptAction = acceptAction;
            this.cancelAction = cancelAction;
            base.layer = layer;
            if (buttonAText.NullOrEmpty())
            {
                this.buttonAText = "OK".Translate();
            }
            forcePause = true;
            absorbInputAroundWindow = true;
            creationRealTime = RealTime.LastRealTime;
            onlyOneOfTypeAllowed = false;
            bool flag = buttonAAction == null && buttonBAction == null && buttonCAction == null;
            forceCatchAcceptAndCancelEventEvenIfUnfocused = acceptAction != null || cancelAction != null || flag;
            closeOnAccept = flag;
            closeOnCancel = flag;
        }
        public override void DoWindowContents(Rect inRect)
        {
            GUI.DrawTexture(new Rect(0, 0, inRect.width, inRect.height - 40f), background);
            float num = inRect.y;
            if (!title.NullOrEmpty())
            {
                Text.Font = GameFont.Medium;
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 0.9f, 0.2f);//标题颜色
                Widgets.Label(new Rect(0f, num, inRect.width, 42f), title);
                GUI.color = oldColor;
                num += 42f;
                num += 10f;
            }
            
            if (image != null)//绘制图片，就是模组封面
            {
                float num2 = (float)image.width / (float)image.height;
                float num3 = 270f * num2;
                GUI.DrawTexture(new Rect(inRect.x + (inRect.width - num3) / 2f, num, num3, 270f), image);
                num += 280f;
                num += 15;
            }

            Text.Font = GameFont.Small;
            Rect outRect = new Rect(inRect.x, num, inRect.width, inRect.height - 35f - 65f - num);
            float width = outRect.width - 16f;
            Rect viewRect = new Rect(0f, 0f, width, Text.CalcHeight(text, width));
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            Color oldColor1 = GUI.color;
            GUI.color = new Color(0f, 1f, 1f);//正文颜色
            Widgets.Label(new Rect(0f, 0f, viewRect.width, viewRect.height), text);
            GUI.color = oldColor1;
            Widgets.EndScrollView();
            int num4 = (buttonCText.NullOrEmpty() ? 2 : 3);
            float num5 = inRect.width / (float)num4;
            float width2 = num5 - 10f;
            if (buttonADestructive)
            {
                GUI.color = new Color(1f, 0.3f, 0.35f);
            }
            string label = (InteractionDelayExpired ? buttonAText : (buttonAText + "(" + Mathf.Ceil(TimeUntilInteractive).ToString("F0") + ")"));
            if (Widgets.ButtonText(new Rect(num5 * (float)(num4 - 1) + 10f, inRect.height - 35f, width2, 35f), label) && InteractionDelayExpired)
            {
                if (buttonAAction != null)
                {
                    buttonAAction();
                }
                Close();
            }
            GUI.color = Color.white;
            if (buttonBText != null && Widgets.ButtonText(new Rect(0f, inRect.height - 35f, width2, 35f), buttonBText))
            {
                if (buttonBAction != null)
                {
                    buttonBAction();
                }
                Close();
            }
            if (buttonCText != null && Widgets.ButtonText(new Rect(num5, inRect.height - 35f, width2, 35f), buttonCText))
            {
                if (buttonCAction != null)
                {
                    buttonCAction();
                }
                if (buttonCClose)
                {
                    Close();
                }
            }


        }
    }
}
