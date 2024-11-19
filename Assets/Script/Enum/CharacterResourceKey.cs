using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public enum CharacterResourceKey
{
    None,
        
    Skill_Cooldown = 1,
    Character_Basic_01 = 10,
    Character_Basic_02 = 11,
}

[Flags]
public enum CharacterResourceResetBy
{
    None,
    StageChanged = 1 << 0,
}

