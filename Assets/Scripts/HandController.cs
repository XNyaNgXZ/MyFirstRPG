using UnityEngine;

public class HandController : MonoBehaviour
{
    [Header("—сылки Ч назначь в Inspector")]
    public Animator handsAnimator;
    public Transform weaponHolder;
    public Inventory inventory;

    private GameObject currentWeaponModel;
    private bool isAttacking = false;

    public static HandController Instance { get; private set; }

    void Awake() => Instance = this;

    void Start()
    {
        if (inventory == null)
            inventory = GetComponent<Inventory>();
        // —брос триггеров, чтобы анимации не стартовали сами
        handsAnimator.ResetTrigger("punch");
        handsAnimator.ResetTrigger("attack");
        handsAnimator.ResetTrigger("pickup");
    }

    void Update()
    {
        if (InventoryUICode.IsOpen || EquipmentUI.IsOpen) return;

        // ќбновл€ем параметр оружи€
        bool hasWeapon = inventory != null &&
                         inventory.GetEquippedItem("Weapon") != null;
        handsAnimator.SetBool("hasWeapon", hasWeapon);

        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            isAttacking = true;
            handsAnimator.SetTrigger(hasWeapon ? "attack" : "punch");
            Invoke(nameof(ResetAttack), 0.5f);
        }
    }

    void ResetAttack() => isAttacking = false;

    // јнимаци€ подбора Ч вызываетс€ из MouseInteractor
    public void PlayPickup()
    {
        handsAnimator.SetTrigger("pickup");
    }

    public void ShowWeaponModel()
    {
        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        currentWeaponModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentWeaponModel.transform.SetParent(weaponHolder, false);
        currentWeaponModel.transform.localPosition = Vector3.zero;
        currentWeaponModel.transform.localScale = new Vector3(0.04f, 0.04f, 0.45f);
        currentWeaponModel.layer = LayerMask.NameToLayer("Hands");
        Destroy(currentWeaponModel.GetComponent<Collider>());
    }

    public void HideWeaponModel()
    {
        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);
        currentWeaponModel = null;
    }
}