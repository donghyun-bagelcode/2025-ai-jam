using System;
using UnityEngine;
using UnityEngine.UIElements;

public class WorldSubCharacter
{
    public Action onRemove;
    public Action onRefresh;

    private TextField name;
    private TextField age;
    private TextField gender;
    private TextField occupation;
    private TextField background;

    private TextField hairColor;
    private TextField hairStyle;
    private TextField eyeColor;
    private TextField skinColor;
    private TextField traits;
    private TextField outfit;
    private TextField physique;

    private TextField defaultPersonality;
    private TextField charmPoint;
    private TextField weakness;

    public WorldSubCharacter(TemplateContainer templateContainer)
    {
        var removeButton = templateContainer.Q<Button>("remove");
        removeButton.clicked += () =>
        {
            onRemove?.Invoke();
        };

        var refeshButton = templateContainer.Q<Button>("refresh");
        refeshButton.clicked += () =>
        {
            // 새로고침 클릭
            onRefresh?.Invoke();
        };

        name = templateContainer.Q<TextField>("name");
        age = templateContainer.Q<TextField>("age");
        gender = templateContainer.Q<TextField>("gender");
        occupation = templateContainer.Q<TextField>("occupation");
        background = templateContainer.Q<TextField>("background");

        hairColor = templateContainer.Q<TextField>("hair-color");
        hairStyle = templateContainer.Q<TextField>("hair-style");
        eyeColor = templateContainer.Q<TextField>("eye-color");
        skinColor = templateContainer.Q<TextField>("skin-color");
        traits = templateContainer.Q<TextField>("traits");
        outfit = templateContainer.Q<TextField>("outfit");
        physique = templateContainer.Q<TextField>("physique");

        defaultPersonality = templateContainer.Q<TextField>("default-personality");
        charmPoint = templateContainer.Q<TextField>("charm-point");
        weakness = templateContainer.Q<TextField>("weakness");
    }

    public override string ToString()
    {
        return $"[Basic Information]\n{basicInformation}\n" +
                $"[Appearance]\n{appearance}\n" +
                $"[Personality]\n{personality}\n";
    }

    public WorldCharacter.BasicInformation basicInformation
    {
        get
        {
            return new WorldCharacter.BasicInformation
            {
                name = name.value,
                age = age.value,
                gender = gender.value,
                occupation = occupation.value,
                background = background.value
            };
        }
    }

    public WorldCharacter.Appearance appearance
    {
        get
        {
            return new WorldCharacter.Appearance
            {
                hairColor = hairColor.value,
                hairStyle = hairStyle.value,
                eyeColor = eyeColor.value,
                skinColor = skinColor.value,
                traits = traits.value,
                outfit = outfit.value,
                physique = physique.value
            };
        }
    }

    public WorldCharacter.Personality personality
    {
        get
        {
            return new WorldCharacter.Personality
            {
                defaultPersonality = defaultPersonality.value,
                charmPoint = charmPoint.value,
                weakness = weakness.value
            };
        }
    }

    public void UpdateUIFromWorldCharacter(WorldCharacter character)
    {
        // 기본 정보 업데이트
        name.value = character.basicInformation.name;
        age.value = character.basicInformation.age;
        gender.value = character.basicInformation.gender;
        occupation.value = character.basicInformation.occupation;
        background.value = character.basicInformation.background;

        // 외모 정보 업데이트
        hairColor.value = character.appearance.hairColor;
        hairStyle.value = character.appearance.hairStyle;
        eyeColor.value = character.appearance.eyeColor;
        skinColor.value = character.appearance.skinColor;
        traits.value = character.appearance.traits;
        outfit.value = character.appearance.outfit;
        physique.value = character.appearance.physique;

        // 성격 정보 업데이트
        defaultPersonality.value = character.personality.defaultPersonality;
        charmPoint.value = character.personality.charmPoint;
        weakness.value = character.personality.weakness;
    }
}