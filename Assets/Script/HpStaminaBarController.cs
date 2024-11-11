using UnityEngine;
using UnityEngine.UI;

public class HpStaminaBarController : MonoBehaviour
{
    public Slider hpSlider;
    public Slider staminaSlider;
    public CharacterBehaviour target; // 캐릭터의 Transform

    Vector3 offset;

    public void UpdateHP(float currentHP, float maxHP)
    {
        hpSlider.value = currentHP / maxHP;
    }

    public void UpdateStamina(float currentStamina, float maxStamina)
    {
        staminaSlider.value = currentStamina / maxStamina;
    }

    public void LateUpdate()
    {
        if (target == null)
        {
            return;
        }
        transform.position = target.transform.position + Vector3.up + new Vector3(0,target.characterData.colliderRadius*2,0);
        // 체력바가 항상 카메라를 바라보도록 설정
        transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        UpdateHP(target.currentHealth, target.characterData.baseHP);

        float currentStamina = target.resourceTable.GetResourceValue(CharacterResourceKey.Skill_Cooldown);
        float maxStamina = target.resourceTable.GetResourceMaxValue(CharacterResourceKey.Skill_Cooldown);
        UpdateStamina(currentStamina, maxStamina);
    }

    public void Despawn(float despawnDelay)
    {
        Destroy(gameObject, despawnDelay);
    }
}
