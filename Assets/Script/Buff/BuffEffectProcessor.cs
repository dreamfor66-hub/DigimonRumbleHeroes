//using System.Collections.Generic;
//using UnityEngine;

//public static class BuffEffectProcessor
//{
//    // BuffEffectData�� HitData�� �����ϴ� �޼���
//    public static void ApplyEffectToHit(HitData hitData, List<BuffEffectData> effects)
//    {
//        foreach (var effect in effects)
//        {
//            // Hit Ÿ�� ������ üũ�Ͽ� ���� ���ǿ� �´��� Ȯ��
//            if (effect.FilterType == HitFilterType.HitType && effect.RequiredHitType != hitData.HitType)
//            {
//                continue;
//            }

//            // ���� ȿ�� ����
//            switch (effect.Type)
//            {
//                case BuffEffectType.Hit_AttackPowerPercent:
//                    hitData.HitDamage *= 1 + (effect.Value / 100f); // ���ݷ� ���� ����
//                    break;

//                    // �߰� ȿ�� ������ ���⿡ �߰�
//            }
//        }
//    }
//}
