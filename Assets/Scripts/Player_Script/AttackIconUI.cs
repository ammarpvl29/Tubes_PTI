using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AttackIconUI : MonoBehaviour
{
    [System.Serializable]
    public class AttackIcon
    {
        public Image iconImage;
        public Image cooldownOverlay;
        public TextMeshProUGUI cooldownText;
        [HideInInspector] public float currentCooldown;
    }

    [Header("Attack Icons")]
    public AttackIcon basicAttackIcon;
    public AttackIcon specialAttackIcon;
    public AttackIcon hollowPurpleIcon;
    public AttackIcon ultimateAttackIcon;

    // Reference to PlayerAttack script
    private PlayerAttack playerAttack;

    private void Start()
    {
        playerAttack = FindObjectOfType<PlayerAttack>();

        // Initialize all cooldown overlays
        InitializeIcon(basicAttackIcon);
        InitializeIcon(specialAttackIcon);
        InitializeIcon(hollowPurpleIcon);
        InitializeIcon(ultimateAttackIcon);
    }

    private void Update()
    {
        if (EnhancedPauseManager.Instance.IsPaused)
            return;

        UpdateIconCooldown(basicAttackIcon, playerAttack.IsBasicAttackOnCooldown(), playerAttack.GetBasicAttackCooldown());
        UpdateIconCooldown(specialAttackIcon, playerAttack.IsSpecialAttackOnCooldown(), playerAttack.GetSpecialAttackCooldown());
        UpdateIconCooldown(ultimateAttackIcon, playerAttack.IsUltimateAttackOnCooldown(), playerAttack.GetUltimateAttackCooldown());

        // Update Hollow Purple icon
        if (playerAttack.IsHollowPurpleAvailable())
        {
            hollowPurpleIcon.iconImage.color = Color.white; // Normal color
            UpdateIconCooldown(hollowPurpleIcon, playerAttack.IsHollowPurpleOnCooldown(), playerAttack.GetHollowPurpleCooldown());
        }
        else
        {
            hollowPurpleIcon.iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grayed out
            hollowPurpleIcon.cooldownOverlay.fillAmount = 1f; // Full overlay
            if (hollowPurpleIcon.cooldownText != null)
                hollowPurpleIcon.cooldownText.gameObject.SetActive(false);
        }
    }

    private void InitializeIcon(AttackIcon icon)
    {
        if (icon.cooldownOverlay != null)
        {
            icon.cooldownOverlay.type = Image.Type.Filled;
            icon.cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            icon.cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
            icon.cooldownOverlay.fillAmount = 0f;
        }
    }

    private void UpdateIconCooldown(AttackIcon icon, bool isOnCooldown, float maxCooldown)
    {
        if (icon.cooldownOverlay == null) return;

        if (isOnCooldown)
        {
            icon.currentCooldown += Time.deltaTime;
            float fillAmount = 1f - (icon.currentCooldown / maxCooldown);
            icon.cooldownOverlay.fillAmount = fillAmount;

            if (icon.cooldownText != null)
            {
                float remainingTime = maxCooldown - icon.currentCooldown;
                icon.cooldownText.text = remainingTime.ToString("0.0");
                icon.cooldownText.gameObject.SetActive(true);
            }

            if (icon.currentCooldown >= maxCooldown)
            {
                icon.currentCooldown = 0f;
                if (icon.cooldownText != null)
                    icon.cooldownText.gameObject.SetActive(false);
            }
        }
        else
        {
            icon.cooldownOverlay.fillAmount = 0f;
            icon.currentCooldown = 0f;
            if (icon.cooldownText != null)
                icon.cooldownText.gameObject.SetActive(false);
        }
    }
}