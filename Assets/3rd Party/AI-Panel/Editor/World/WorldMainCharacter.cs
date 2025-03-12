using System;
using UnityEngine;
using UnityEngine.UIElements;

public class WorldMainCharacter
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

    private TextField belief;
    private TextField motivation;
    private TextField goals;
    private TextField conflict;

    private TextField allies;
    private TextField enemies;
    private TextField family;
    private TextField friends;
    private TextField acquaintance;
    private TextField socials;
    private TextField loveInterest;

    public WorldMainCharacter(TemplateContainer templateContainer)
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

        belief = templateContainer.Q<TextField>("belief");
        motivation = templateContainer.Q<TextField>("motivation");
        goals = templateContainer.Q<TextField>("goals");
        conflict = templateContainer.Q<TextField>("conflict");

        allies = templateContainer.Q<TextField>("allies");
        enemies = templateContainer.Q<TextField>("enemies");
        family = templateContainer.Q<TextField>("family");
        friends = templateContainer.Q<TextField>("friends");
        acquaintance = templateContainer.Q<TextField>("acquaintance");
        socials = templateContainer.Q<TextField>("socials");
        loveInterest = templateContainer.Q<TextField>("love-interest");
    }

    public override string ToString()
    {
        return $"[Basic Information]\n{basicInformation}\n" +
                $"[Appearance]\n{appearance}\n" +
                $"[Personality]\n{personality}\n" +
                $"[Values]\n{values}\n" +
                $"[Relationship]\n{relationship}";
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

    public WorldCharacter.Values values
    {
        get
        {
            return new WorldCharacter.Values
            {
                belief = belief.value,
                motivation = motivation.value,
                goals = goals.value,
                conflict = conflict.value
            };
        }
    }

    public WorldCharacter.Relationship relationship
    {
        get
        {
            return new WorldCharacter.Relationship
            {
                allies = allies.value,
                enemies = enemies.value,
                family = family.value,
                friends = friends.value,
                acquaintance = acquaintance.value,
                socials = socials.value,
                loveInterest = loveInterest.value
            };
        }
    }

    public void UpdateUIFromWorldCharacter(WorldCharacter character)
    {
        if (character == null)
        {
            Debug.LogError("WorldCharacter is null. Skipping UI update.");
            return;
        }

        // Helper method to handle null-safe assignments
        void SafeUpdate(TextField field, string value, string fallback, string fieldName)
        {
            if (field == null)
            {
                Debug.LogWarning($"{fieldName} TextField is not initialized.");
                return;
            }
            try
            {
                field.value = value ?? fallback;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error updating {field.name}: {ex.Message}");
            }
        }

        // 기본 정보 업데이트
        SafeUpdate(name, character.basicInformation?.name, "Unknown", "Name");
        SafeUpdate(age, character.basicInformation?.age, "0", "Age");
        SafeUpdate(gender, character.basicInformation?.gender, "Unspecified", "Gender");
        SafeUpdate(occupation, character.basicInformation?.occupation, "Unemployed", "Occupation");
        SafeUpdate(background, character.basicInformation?.background, "No background information.", "Background");

        // 외모 정보 업데이트
        SafeUpdate(hairColor, character.appearance?.hairColor, "Unknown", "Hair Color");
        SafeUpdate(hairStyle, character.appearance?.hairStyle, "Unknown", "Hair Style");
        SafeUpdate(eyeColor, character.appearance?.eyeColor, "Unknown", "Eye Color");
        SafeUpdate(skinColor, character.appearance?.skinColor, "Unknown", "Skin Color");
        SafeUpdate(traits, character.appearance?.traits, "No traits specified.", "Traits");
        SafeUpdate(outfit, character.appearance?.outfit, "No outfit specified.", "Outfit");
        SafeUpdate(physique, character.appearance?.physique, "No physique specified.", "Physique");

        // 성격 정보 업데이트
        SafeUpdate(defaultPersonality, character.personality?.defaultPersonality, "Unknown", "Default Personality");
        SafeUpdate(charmPoint, character.personality?.charmPoint, "None", "Charm Point");
        SafeUpdate(weakness, character.personality?.weakness, "None", "Weakness");

        // 가치관 정보 업데이트
        SafeUpdate(belief, character.values?.belief, "None", "Belief");
        SafeUpdate(motivation, character.values?.motivation, "None", "Motivation");
        SafeUpdate(goals, character.values?.goals, "No goals specified.", "Goals");
        SafeUpdate(conflict, character.values?.conflict, "No conflicts specified.", "Conflict");

        // 관계 정보 업데이트
        SafeUpdate(allies, character.relationship?.allies, "None", "Allies");
        SafeUpdate(enemies, character.relationship?.enemies, "None", "Enemies");
        SafeUpdate(family, character.relationship?.family, "None", "Family");
        SafeUpdate(friends, character.relationship?.friends, "None", "Friends");
        SafeUpdate(acquaintance, character.relationship?.acquaintance, "None", "Acquaintance");
        SafeUpdate(socials, character.relationship?.socials, "None", "Socials");
        SafeUpdate(loveInterest, character.relationship?.loveInterest, "None", "Love Interest");
    }
}