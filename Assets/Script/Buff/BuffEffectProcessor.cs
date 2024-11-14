//using System.Collections.Generic;
//using UnityEngine;

//public static class BuffEffectProcessor
//{
//    // BuffEffectData를 HitData에 적용하는 메서드
//    public static void ApplyEffectToHit(HitData hitData, List<BuffEffectData> effects)
//    {
//        foreach (var effect in effects)
//        {
//            if (effect.FilterType == HitFilterType.HitType && effect.RequiredHitType != hitData.HitType)
//            {
//                continue;
//            }

//            switch (effect.Type)
//            {
//                case BuffEffectType.Hit_AttackPowerPercent:
//                    hitData.HitDamage *= 1 + (effect.Value / 100f); // 공격력 증가 적용
//                    break;

//                    // 추가 효과 유형을 여기에 추가
//            }
//        }
//    }
//}
