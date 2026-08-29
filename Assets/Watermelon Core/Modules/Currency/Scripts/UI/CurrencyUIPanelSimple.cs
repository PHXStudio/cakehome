using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    [System.Serializable]
    public class CurrencyUIPanelSimple : MonoBehaviour
    {
        [SerializeField] CurrencyType currencyType;

        [Space]
        [SerializeField] bool updateOnChange = true;
        [SerializeField] bool useFormattedAmount = true;

        [Space]
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] Image icon;
        [SerializeField] Button addButton;

        public string Text { get => text.text; set => text.text = value; }
        public Sprite Icon { get => icon.sprite; set => icon.sprite = value; }

        public Image Image => icon;
        public Button AddButton => addButton;

        private Currency currency;
        public Currency Currency => currency;
        
        private RectTransform rectTransformRef;
        public RectTransform RectTransform => rectTransformRef;

        public RectTransform TextRectTransform => text.rectTransform;

        private bool isInitialized;
        private bool isSubscribed;

        private void Awake()
        {
            rectTransformRef = GetComponent<RectTransform>();

            Init();
        }

        // Currency 挂在静态 CurrencyController 上、生命周期长于页面 —— 隐藏/销毁时必须反订阅，
        // 否则货币变动会回调到已销毁的 MonoBehaviour（MissingReferenceException + 泄漏）。
        // OnEnable 里重新订阅并刷新，覆盖 SetActive(false)→(true) 的页面切换路径。
        private void OnEnable()
        {
            if (!isInitialized) return;
            Activate();
            Redraw();
        }

        private void OnDisable()
        {
            if (isInitialized) Disable();
        }

        public void Init()
        {
            if (isInitialized) return;

            currency = CurrencyController.GetCurrency(currencyType);

            icon.sprite = currency.Icon;

            isInitialized = true;

            Redraw();
            Activate();
        }

        public void Init(CurrencyType currencyType)
        {
            if(isInitialized)
            {
                isInitialized = false;

                Disable();
            }

            this.currencyType = currencyType;

            Init();
        }

        public void Redraw()
        {
            text.text = useFormattedAmount ? currency.AmountFormatted : currency.Amount.ToString();
        }

        public void SetAmount(int amount, bool format = true)
        {
            text.text = format ? CurrencyHelper.Format(amount) : amount.ToString();
        }

        public void Activate()
        {
            // 幂等：Awake→Init→Activate 之后紧接着的 OnEnable 会再调一次，不能重复订阅。
            // 页面系统用 canvas.enabled 切换显隐（GameObject 保持 active，OnEnable 不触发），
            // 隐藏期间面板处于退订状态、会错过货币变动 —— 重新订阅时必须补一次 Redraw，
            // 否则文本停留在隐藏前的旧值（如通关奖励到账后金币栏不刷新）。
            if (updateOnChange && !isSubscribed)
            {
                currency.OnCurrencyChanged += OnCurrencyAmountChanged;
                isSubscribed = true;

                Redraw();
            }
        }

        public void Disable()
        {
            if (updateOnChange && isSubscribed)
            {
                currency.OnCurrencyChanged -= OnCurrencyAmountChanged;
                isSubscribed = false;
            }
        }

        private void OnCurrencyAmountChanged(Currency currency, int amountDifference)
        {
            text.text = useFormattedAmount ? currency.AmountFormatted : currency.Amount.ToString();
        }
    }
}