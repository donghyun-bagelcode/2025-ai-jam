using System;

[Serializable]
public class WorldCharacter
{
    // 캐릭터 데이터 속성
    public BasicInformation basicInformation;
    public Appearance appearance;
    public Personality personality;
    public Values values;
    public Relationship relationship;
    
    [Serializable]
    public class BasicInformation
    {
        public string name;
        public string age;
        public string gender;
        public string occupation;
        public string background;

        public override string ToString()
        {
            return $"Name: {name}\n" +
                    $"Age: {age}\n" +
                    $"Gender: {gender}\n" +
                    $"Occupation: {occupation}\n" +
                    $"Background: {background}";
        }
    }

    [Serializable]
    public class Appearance
    {
        public string hairColor;
        public string hairStyle;
        public string eyeColor;
        public string skinColor;
        public string traits;
        public string outfit;
        public string physique;

        public override string ToString()
        {
            return $"Hair Color: {hairColor}\n" +
                    $"Hair Style: {hairStyle}\n" +
                    $"Eye Color: {eyeColor}\n" +
                    $"Skin Color: {skinColor}\n" +
                    $"Traits: {traits}\n" +
                    $"Outfit: {outfit}\n" +
                    $"Physique: {physique}";
        }
    }

    [Serializable]
    public class Personality
    {
        public string defaultPersonality;
        public string charmPoint;
        public string weakness;

        public override string ToString()
        {
            return $"Default Personality: {defaultPersonality}\n" +
                    $"Charm Point: {charmPoint}\n" +
                    $"Weakness: {weakness}";
        }
    }

    [Serializable]
    public class Values
    {
        public string belief;
        public string motivation;
        public string goals;
        public string conflict;

        public override string ToString()
        {
            return $"Belief: {belief}\n" +
                    $"Motivation: {motivation}\n" +
                    $"Goals: {goals}\n" +
                    $"Conflict: {conflict}";
        }
    }

    [Serializable]
    public class Relationship
    {
        public string allies;
        public string enemies;
        public string family;
        public string friends;
        public string acquaintance;
        public string socials;
        public string loveInterest;

        public override string ToString()
        {
            return $"Allies: {allies}\n" +
                    $"Enemies: {enemies}\n" +
                    $"Family: {family}\n" +
                    $"Friends: {friends}\n" +
                    $"Acquaintance: {acquaintance}\n" +
                    $"Socials: {socials}\n" +
                    $"Love Interest: {loveInterest}";
        }
    }
}