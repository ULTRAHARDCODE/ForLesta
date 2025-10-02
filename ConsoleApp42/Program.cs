using System.Numerics;
using static System.Net.Mime.MediaTypeNames;
using static Profession;
Battle battle = new();
battle.StartGame();

static class AbilitiesLibrary
{
    public static readonly AttackAbbility Warrior1 = new("Порыв к действию", (attacker, defender) =>
    {
        if (attacker.CurrentTurn == 1 && attacker is Hero hero)
        {
            return hero.Weapon?.Damage ?? 0;
        }
        else return 0;
    });
    public static readonly DeffenceAbbility Warrior2 = new("Щит", (attacker, defender) =>
    {
        if (defender.Strength > attacker.Strength)
        {
            return -3;
        }
        else return 0;
    });
    public static readonly PassiveAbbility Warrior3 = new("Увеличение силы", (attacker, defender) =>
    {
        return 1;
    }, PassiveAbbility.BonusType.Strength);

    public static readonly AttackAbbility Rogue1 = new("Скрытая атака", (attacker, defender) =>
    {
        if (attacker.Dexterity > defender.Dexterity)
        {
            return 1;
        }
        else return 0;

    });
    public static readonly PassiveAbbility Rogue2 = new("Увеличение ловкости", (attacker, defender) =>
    {
        return 1;
    }, PassiveAbbility.BonusType.Dexterity);
    public static readonly AttackAbbility Rogue3 = new("Яд", (attacker, defender) =>
    {
        if (attacker.CurrentTurn > 1)
        {
            int poisonDamage = attacker.CurrentTurn - 1;
            return poisonDamage;
        }
        else return 0;
    });

    public static readonly AttackAbbility Barbarian1 = new("Ярость", (attacker, defender) =>
    {
        if (attacker.CurrentTurn < 4)
        {
            return 2;
        }
        else return -1;
    });
    public static readonly DeffenceAbbility Barbarian2 = new("Каменная кожа", (attacker, defender) =>
    {
        return defender.Stamina;
    });
    public static readonly PassiveAbbility Barbarian3 = new("Увеличение выносливости", (attacker, defender) =>
    {
        return 1;
    }, PassiveAbbility.BonusType.Stamina);

    public static readonly DeffenceAbbility Skeleton1 = new("Уязвимость к дробящему урону", (attacker, defender) =>
    {
        if (attacker is Hero hero)
        {
            if (hero.Weapon?.TypeDamage == TypeDamage.bludgeoningDamage)
            {
                return -hero.Attack();
            }
        }
        return 0;
    });
    public static readonly DeffenceAbbility Slime1 = new("Насмешка над острием", (attacker, defender) =>
    {
        if (attacker is Hero hero)
        {
            if (hero.Weapon?.TypeDamage == TypeDamage.slashingDamage)
            {
                return -hero.Weapon.Damage;
            }
        }
        return 0;
    });
    public static readonly AttackAbbility Ghost1 = Rogue1;
    public static readonly DeffenceAbbility Golem1 = Barbarian2;
    public static readonly AttackAbbility Dragon1 = new("Огненное дыхание", (attacker, defender) =>
    {
        if (attacker.CurrentTurn % 3 == 0)
        {
            return 3;
        }
        else return 0;
    });
} // Cписок умений

interface IPlayer // Интерфейс участника битвы
{
    public int Attack();

    int CurrentTurn { get; }
    int CurrentHealth { get; }
    int Dexterity { get; }
    int Strength { get; }
    int Stamina { get; }

    void TakeDamage(int damage);
    void EndTurn();

    public List<Abbility> GetListAbbilities();
}

class Hero : IPlayer
{
    public int CurrentTurn { get; protected set; } = 1;
    public int Health { get; protected set; }
    public int CurrentHealth { get; protected set; }
    public int Strength { get; protected set; }
    public int Dexterity { get; protected set; }
    public int Stamina { get; protected set; }
    public int HeroLevel { get; protected set; } = 0;

    private readonly List<Abbility> CurrentAbbility = [];
    private readonly List<Profession> professions = [];

    public Weapon? Weapon { get; protected set; }
    public event Action<IPlayer>? OnSizeDamage;
    public event Action<Profession.PassiveAbbility, int, IPlayer>? OnChangeAfterPassiveAbbility;
    internal Hero()
    {
        OnSizeDamage += GameUI.SizeDamage;
        OnChangeAfterPassiveAbbility += GameUI.ChangeAfterPassiveAbbility;

        Random rnd = new();
        Strength = rnd.Next(1, 4);
        Dexterity = rnd.Next(1, 4);
        Stamina = rnd.Next(1, 4);
    }// Создание базовых атрибутов



    private void UpdateAbbility()
    {
        CurrentAbbility.Clear();
        foreach (var profession in professions)
        {
            CurrentAbbility.AddRange(profession.GetAbbilitities());
        }
    } // Обновление списка способностей героя на основе профессий
    internal void UpdateStats()
    {
        ApplyPassiveAbbilities();
        CurrentHealth = Health;
        CurrentTurn = 1;

    } // Обновление атрибутов героя на основе профессий + Сброс счетчика ходов

    internal void LevelUp(Profession chosenProfession)
    {

        if (HeroLevel == 0)
        {
            Weapon = chosenProfession is Warrior ? new Sword() :
                      chosenProfession is Barbarian ? new Club() :
                      chosenProfession is Rogue ? new Dagger() : null;
        }

        var existingProfession = professions.FirstOrDefault(p =>
            (p is Warrior && chosenProfession is Warrior) ||
            (p is Barbarian && chosenProfession is Barbarian) ||
            (p is Rogue && chosenProfession is Rogue));

        if (existingProfession == null)
        {
            chosenProfession.LevelUp();
            professions.Add(chosenProfession);
        }
        else
        {
            existingProfession.LevelUp();
        }

        PropertyHeroLevel++;
        Health += GetHealthForLevel(chosenProfession) + Stamina;
        UpdateAbbility();
        UpdateStats();


    } // Повышение уровня героя и выбранной профессии
    private int PropertyHeroLevel
    {
        get { return HeroLevel; }
        set
        {
            if (value <= 3)
            {
                HeroLevel = value;
            }

        }
    }   //  Ограничение уровня героя до 3

    private static int GetHealthForLevel(Profession profession) => profession switch
    {
        Rogue => 4,
        Warrior => 5,
        Barbarian => 6,
        _ => -1
    }; // Здоровье за уровень в зависимости от профессии

    internal void ShowFullStats()
    {
        Console.WriteLine("=======================================");
        Console.WriteLine("               HERO STATS              ");
        Console.WriteLine("=======================================");
        Console.WriteLine($" Level       : {HeroLevel}");
        Console.WriteLine($" Health      : {CurrentHealth}/{Health}");
        Console.WriteLine($" Strength    : {Strength}");
        Console.WriteLine($" Dexterity   : {Dexterity}");
        Console.WriteLine($" Stamina     : {Stamina}");
        Console.WriteLine($" Weapon      : {(Weapon != null ? $"{Weapon.GetType().Name} (Урон: {Weapon.Damage}, Тип: {Weapon.TypeDamage})" : "None")}");
        Console.WriteLine("---------------------------------------");

        if (professions.Count == 0)
        {
            Console.WriteLine("Профессии: отсутствуют");
        }
        else
        {
            Console.WriteLine("Профессии героя:");
            foreach (var profession in professions)
            {
                Console.WriteLine($" - {profession.GetType().Name} уровень {profession.ShowLevel()}");
            }
        }

        Console.WriteLine("---------------------------------------");
        Console.WriteLine("Умения героя:");
        if (CurrentAbbility.Count == 0)
        {
            Console.WriteLine(" - отсутствуют");
        }
        else
        {
            foreach (var abb in CurrentAbbility)
            {
                Console.WriteLine($" - {abb}");
            }
        }
        Console.WriteLine("=======================================\n");
    }

    public int Attack()
    {

        if (Weapon != null)
        {
            OnSizeDamage?.Invoke(this);
            return Strength + Weapon.Damage;
        }
        else
        {
            OnSizeDamage?.Invoke(this);
            return Strength;
        }
    } // Атака
    public void TakeDamage(int damage)
    {
        if (damage > 0)
            CurrentHealth -= damage;

    } // Получение урона
    public List<Abbility> GetListAbbilities()
    {
        return CurrentAbbility.Where(a => a is AttackAbbility || a is DeffenceAbbility).ToList();
    } // Получение способностей
    private void ApplyPassiveAbbilities()
    {
        foreach (var abbility in CurrentAbbility.OfType<PassiveAbbility>())
        {
            int bonus = abbility.Effect(this, this);
            switch (abbility.Target)
            {
                case PassiveAbbility.BonusType.Strength:
                    Strength += bonus;
                    break;
                case PassiveAbbility.BonusType.Dexterity:
                    Dexterity += bonus;
                    break;
                case PassiveAbbility.BonusType.Stamina:
                    Stamina += bonus;
                    break;
            }
            OnChangeAfterPassiveAbbility?.Invoke(abbility, bonus, this);
        }
    } // Применение способностей
    public void HealAfterBattle()
    {
        CurrentHealth = Health;
    } // Восстановление здоровья после боя
    public void EndTurn()
    {
        CurrentTurn++;
    }
    internal void ChangeWeapon(Enemy enemy)
    {
        Weapon = enemy.RewardWeapon;
    }
} // Герой

abstract class Profession // Профессии
{
    protected int level;

    internal void LevelUp()
    {
        level++;
    } // Уровень профессии
    public int ShowLevel()
    {
        return level;
    }


    public abstract class Abbility
    {
        string Name { get; }
        public override string ToString() => Name;
        public Func<IPlayer, IPlayer, int> Effect { get; }
        public Abbility(string name, Func<IPlayer, IPlayer, int> effect)
        {
            Name = name;
            Effect = effect;
        }
    }
    public class AttackAbbility : Abbility
    {
        public AttackAbbility(string name, Func<IPlayer, IPlayer, int> effect) : base(name, effect)
        {

        }
    }
    public class DeffenceAbbility : Abbility
    {
        public DeffenceAbbility(string name, Func<IPlayer, IPlayer, int> effect) : base(name, effect)
        {

        }
    }
    public class PassiveAbbility : Abbility
    {
        public enum BonusType { Strength, Dexterity, Stamina };
        public BonusType Target { get; }
        public PassiveAbbility(string name, Func<IPlayer, IPlayer, int> effect, BonusType target) : base(name, effect)
        {
            Target = target;
        }
    }
    public abstract List<Abbility> GetAbbilitities();
}

// Конкретные классы профессий
class Warrior : Profession
{
    public override List<Abbility> GetAbbilitities()
    {
        return level switch
        {
            1 => [AbilitiesLibrary.Warrior1],
            2 => [AbilitiesLibrary.Warrior1, AbilitiesLibrary.Warrior2],
            3 => [AbilitiesLibrary.Warrior1, AbilitiesLibrary.Warrior2, AbilitiesLibrary.Warrior3],
            _ => [],
        };
    }
}
class Rogue : Profession
{
    public override List<Abbility> GetAbbilitities()
    {
        return level switch
        {
            1 => [AbilitiesLibrary.Rogue1],
            2 => [AbilitiesLibrary.Rogue1, AbilitiesLibrary.Rogue2],
            3 => [AbilitiesLibrary.Rogue1, AbilitiesLibrary.Rogue2, AbilitiesLibrary.Rogue3],
            _ => [],
        };
    }
}
class Barbarian : Profession
{
    public override List<Abbility> GetAbbilitities()
    {
        return level switch
        {
            1 => [AbilitiesLibrary.Barbarian1],
            2 => [AbilitiesLibrary.Barbarian1, AbilitiesLibrary.Barbarian2],
            3 => [AbilitiesLibrary.Barbarian1, AbilitiesLibrary.Barbarian2, AbilitiesLibrary.Barbarian3],
            _ => [],
        };
    }
}

// Оружие
abstract class Weapon
{
    public abstract int Damage { get; }
    public abstract TypeDamage TypeDamage { get; }
}
// Конкретные классы оружия
class Sword : Weapon
{
    public override int Damage => 3;
    public override TypeDamage TypeDamage => TypeDamage.slashingDamage;
}
class Club : Weapon
{
    public override int Damage => 3;
    public override TypeDamage TypeDamage => TypeDamage.bludgeoningDamage;
}
class Dagger : Weapon
{
    public override int Damage => 2;
    public override TypeDamage TypeDamage => TypeDamage.piercingDamage;
}
class Axe : Weapon
{
    public override int Damage => 4;
    public override TypeDamage TypeDamage => TypeDamage.slashingDamage;
}
class Spear : Weapon
{
    public override int Damage => 3;
    public override TypeDamage TypeDamage => TypeDamage.piercingDamage;
}
class LegendarySword : Weapon
{
    public override int Damage => 10;
    public override TypeDamage TypeDamage => TypeDamage.slashingDamage;
}
enum TypeDamage
{
    slashingDamage,
    bludgeoningDamage,
    piercingDamage
}


class Battle
{
    private Hero _hero = new();
    private Enemy? _enemy;

    private IPlayer? AttackPlayer;
    private List<Abbility> _abbilitiesAttacker = [];
    private IPlayer? Defender;
    private List<Abbility> _abbilitiesDefender = [];

    private bool restart = true;
    private int CounterWin;

    private readonly Random rnd = new();


    public event Func<bool>? OnHeroDefeated;
    public event Func<Hero, Profession>? OnChangeProfession;
    public event Action? OnGameEnd;
    public event Action<IPlayer, int> OnDamaged;
    public event Action<IPlayer>? OnFindFirstTurn;
    public event Func<Enemy, bool>? OnChangedWeapon;
    public event Action<IPlayer>? OnEvasion;
    public event Action<IPlayer, IPlayer>? OnComparing;
    public event Action<IPlayer, IPlayer, int>? OnHit;
    public event Action<Abbility, int, IPlayer>? OnChangeAfterAttackAbbility;
    public event Action<Abbility, int, IPlayer>? OnChangeAfterDeffenceAbbility;


    public Battle()
    {
        OnHeroDefeated += GameUI.HandleHeroDefeated;
        OnChangeProfession += GameUI.HandleChangeProfession;
        OnGameEnd += GameUI.HandleGameEnd;
        OnDamaged += GameUI.HandleDamage;
        OnFindFirstTurn += GameUI.HandleFindFirstTurn;
        OnChangedWeapon += GameUI.HandleChangeWeapon;
        OnEvasion += GameUI.HandleEvasion;
        OnComparing += GameUI.Comparing;
        OnHit += GameUI.MissOrAttack;
        OnChangeAfterAttackAbbility += GameUI.ChangeAfterAttackAbbility;
        OnChangeAfterDeffenceAbbility += GameUI.ChangeAfterDeffenceAbbility;

    } // Подписки на события

    public void StartGame()
    {
        _hero.LevelUp(OnChangeProfession?.Invoke(_hero)!);

        while (CounterWin < 5 && restart == true)
        {
            _hero.ShowFullStats();
            _enemy = EnemyFactory.CreateRandomEnemy();
            SetFirstTurn();
            Turns();
            CounterWin++;
        }

        if (CounterWin == 5)
        {
            OnGameEnd?.Invoke();
        }
    } // Запуск игры
    private void SetFirstTurn()
    {
        OnComparing?.Invoke(_hero, _enemy!);
        if (_hero.Dexterity >= _enemy!.Dexterity)
        {
            AttackPlayer = _hero;
            Defender = _enemy;
        }
        else
        {
            AttackPlayer = _enemy;
            Defender = _hero;
        }
        OnFindFirstTurn?.Invoke(AttackPlayer);

    } //  Определение первого хода на основе ловкости

    private void Turns()
    {
        bool takeWeapon;
        while (CounterWin < 5 && restart == true)
        {
            int Evasion = rnd.Next(1, AttackPlayer!.Dexterity + Defender!.Dexterity + 1);
            OnHit?.Invoke(AttackPlayer, Defender, Evasion);
            if (Evasion <= Defender.Dexterity)
            {
                AttackPlayer.EndTurn();
                SwapPlayers();
            } // Уклонение от атаки
            else
            {
                SetActiveAbbilities();
                int bonusDamage = UseActiveAbbilities();
                int damage = AttackPlayer.Attack() + bonusDamage;
                Defender.TakeDamage(damage);
                OnDamaged?.Invoke(Defender, damage);
                if (Defender.CurrentHealth > 0)
                {
                    AttackPlayer.EndTurn();
                    SwapPlayers();
                }
                else
                {
                    if (AttackPlayer is Hero)
                    {
                        takeWeapon = OnChangedWeapon?.Invoke(_enemy!) ?? false;
                        if (takeWeapon && _enemy?.RewardWeapon != null)
                        {
                            _hero.ChangeWeapon(_enemy);
                        }
                        if (_hero.HeroLevel < 3)
                        {
                            _hero.LevelUp(OnChangeProfession?.Invoke(_hero)!);
                        }

                        else
                        {

                            _hero.UpdateStats();
                        }

                        _hero.HealAfterBattle();
                    }
                    else
                    {
                        restart = OnHeroDefeated?.Invoke() ?? false;
                        if (restart)
                        {
                            _hero = new Hero();
                            StartGame();
                        }
                    }
                    break;
                }

            }
        }
        void SwapPlayers()
        {
            (Defender, AttackPlayer) = (AttackPlayer, Defender);
            (_abbilitiesDefender, _abbilitiesAttacker) = (_abbilitiesAttacker, _abbilitiesDefender);
        }
        void SetActiveAbbilities() // Установка активных способностей
        {
            _abbilitiesAttacker = AttackPlayer!.GetListAbbilities();
            _abbilitiesDefender = Defender!.GetListAbbilities();
        }
        int UseActiveAbbilities()
        {
            int bonusDamage = 0;
            foreach (var ability in _abbilitiesAttacker)
            {
                if (ability is AttackAbbility)
                {
                    bonusDamage += ability.Effect(AttackPlayer!, Defender!);
                    if (ability.Effect(AttackPlayer!, Defender!) != 0)
                        OnChangeAfterAttackAbbility?.Invoke(ability, ability.Effect(AttackPlayer!, Defender!), AttackPlayer);
                }
            } // Активация способностей атакующего
            foreach (var ability in _abbilitiesDefender)
            {
                if (ability is DeffenceAbbility)
                {
                    bonusDamage += ability.Effect(AttackPlayer!, Defender!);
                    if (ability.Effect(AttackPlayer!, Defender!) != 0)
                        OnChangeAfterDeffenceAbbility?.Invoke(ability, ability.Effect(AttackPlayer!, Defender!), Defender!);
                }
            } // Активация способностей защищающегося
            return bonusDamage;
        }
    } // Бой
}

// Противники
abstract class Enemy : IPlayer
{
    public int CurrentTurn { get; protected set; } = 1;
    public int Health { get; protected set; }
    public int CurrentHealth { get; protected set; }
    public int Strength { get; protected set; }
    public int Dexterity { get; protected set; }
    public int Stamina { get; protected set; }
    public int Damage { get; protected set; }
    public Weapon? RewardWeapon { get; protected set; }

    public event Action<IPlayer>? OnSizeDamage;
    protected List<Abbility> CurrentAbbility = [];

    internal Enemy()
    {
        OnSizeDamage += GameUI.SizeDamage;
    }
    public int Attack()
    {
        OnSizeDamage?.Invoke(this);
        return Strength + Damage;
    } // Атака
    public void TakeDamage(int damage)
    {
        if (damage > 0)
            CurrentHealth -= damage;
    } // Получение урона
    public void EndTurn()
    {
        CurrentTurn++;
    }

    public List<Abbility> GetListAbbilities()
    {
        return CurrentAbbility.Where(a => a is AttackAbbility || a is DeffenceAbbility).ToList();
    }

}
// Конкретные классы противников
class Goblin : Enemy
{
    public Goblin()
    {
        Health = 5;
        CurrentHealth = 5;
        Damage = 2;
        Strength = 1;
        Dexterity = 1;
        Stamina = 1;
        RewardWeapon = new Dagger();
    }

}
class Skeleton : Enemy
{
    public Skeleton()
    {
        Health = 10;
        CurrentHealth = 10;
        Damage = 2;
        Strength = 2;
        Dexterity = 2;
        Stamina = 1;
        RewardWeapon = new Club();
        CurrentAbbility.Add(AbilitiesLibrary.Skeleton1);
    }
}
class Slime : Enemy
{
    public Slime()
    {
        Health = 8;
        CurrentHealth = 8;
        Damage = 1;
        Strength = 3;
        Dexterity = 1;
        Stamina = 2;

        RewardWeapon = new Spear();

        CurrentAbbility.Add(AbilitiesLibrary.Slime1);
    }
}
class Ghost : Enemy
{
    public Ghost()
    {
        Health = 6;
        CurrentHealth = 6;
        Damage = 3;
        Strength = 1;
        Dexterity = 3;
        Stamina = 1;
        RewardWeapon = new Sword();

        CurrentAbbility.Add(AbilitiesLibrary.Ghost1);
    }
}
class Golem : Enemy
{
    public Golem()
    {
        Health = 10;
        CurrentHealth = 10;
        Damage = 1;
        Strength = 3;
        Dexterity = 1;
        Stamina = 3;
        RewardWeapon = new Axe();

        CurrentAbbility.Add(AbilitiesLibrary.Golem1);
    }
}
class Dragon : Enemy
{
    public Dragon()
    {
        Health = 20;
        CurrentHealth = 20;
        Damage = 4;
        Strength = 3;
        Dexterity = 3;
        Stamina = 3;
        RewardWeapon = new LegendarySword();

        CurrentAbbility.Add(AbilitiesLibrary.Dragon1);
    }
}
static class EnemyFactory
{
    public static Enemy CreateEnemy(EnemyType type) => type switch
    {
        EnemyType.Goblin => new Goblin(),
        EnemyType.Skeleton => new Skeleton(),
        EnemyType.Slime => new Slime(),
        EnemyType.Ghost => new Ghost(),
        EnemyType.Golem => new Golem(),
        EnemyType.Dragon => new Dragon(),
        _ => throw new ArgumentException("Неверный тип врага")
    };

    public static Enemy CreateRandomEnemy()
    {
        Random rnd = new();
        Array values = Enum.GetValues(typeof(EnemyType));
        EnemyType randomType = (EnemyType)values.GetValue(rnd.Next(values.Length))!;
        return CreateEnemy(randomType);
    }
} // Фабрика создания врагов
enum EnemyType
{
    Goblin,
    Skeleton,
    Slime,
    Ghost,
    Golem,
    Dragon
}


class GameUI
{
    private static void WriteLineSeparator() => Console.WriteLine(new string('=', 50));

    public static void HandleFindFirstTurn(IPlayer firstPlayer)
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Ход начинается! {firstPlayer} ходит первым.");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void HandleDamage(IPlayer player, int damage)
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{player} получает {damage} урона! Текущее здоровье: {player.CurrentHealth}");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static Profession HandleChangeProfession(Hero hero)
    {
        if (hero.HeroLevel >= 3) return null!;
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Выберите профессию для прокачки:");
        Console.WriteLine("1 - Warrior");
        Console.WriteLine("2 - Barbarian");
        Console.WriteLine("3 - Rogue");
        Console.ResetColor();

        while (true)
        {
            string? input = Console.ReadLine();
            switch (input)
            {
                case "1": return new Warrior();
                case "2": return new Barbarian();
                case "3": return new Rogue();
                default:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Неверный выбор, попробуйте снова:");
                    Console.ResetColor();
                    continue;
            }
        }
    }

    public static bool HandleHeroDefeated()
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Герой проиграл! Игра окончена.");
        Console.ResetColor();
        Console.WriteLine("Начать заново? (y/n)");

        string? input = Console.ReadLine();
        if (input?.ToLower() == "y")
        {
            Console.Clear();
            return true;
        }
        return false;
    }

    public static void HandleGameEnd()
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Поздравляем! Ты выиграл игру!");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static bool HandleChangeWeapon(Enemy enemy)
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"При убийстве {enemy} найдено оружие: {enemy.RewardWeapon}. Забрать? (y/n)");
        Console.ResetColor();

        string? input = Console.ReadLine();
        return input?.ToLower() == "y" && enemy.RewardWeapon != null;
    }

    public static void HandleEvasion(IPlayer player)
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"{player} успешно уклонился!");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void Comparing(IPlayer first, IPlayer second)
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Сравнение ловкости:");
        Console.WriteLine($"{first}: {first.Dexterity}  vs  {second}: {second.Dexterity}");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void MissOrAttack(IPlayer attacker, IPlayer defender, int evasion)
    {
        WriteLineSeparator();
        Console.WriteLine($"Ход #{attacker.CurrentTurn} {attacker} ");
        Console.WriteLine($"Ловкость {attacker}: {attacker.Dexterity}, Ловкость {defender}: {defender.Dexterity}");
        Console.WriteLine($"Шанс попадания: {evasion}");

        if (evasion > defender.Dexterity)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Попадание! {evasion} > {defender.Dexterity}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Промах! {evasion} <= {defender.Dexterity}");
        }

        Console.ResetColor();
        Console.WriteLine();
    }

    public static void SizeDamage(IPlayer player)
    {
        if (player is Hero hero)
            SizeDamageHero(hero);
        else
            SizeDamageEnemy((Enemy)player);
    }

    private static void SizeDamageHero(Hero hero)
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Сила героя: {hero.Strength}");
        Console.WriteLine($"Оружие: {hero.Weapon}, Урон: {hero.Weapon?.Damage}, Тип: {hero.Weapon?.TypeDamage}");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void SizeDamageEnemy(Enemy enemy)
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Сила врага: {enemy.Strength}");
        Console.WriteLine($"Урон от оружия: {enemy.Damage}, Всего урона: {enemy.Strength + enemy.Damage}");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void ChangeAfterAttackAbbility(Abbility abb, int damage, IPlayer player)
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"У {player} сработала способность! {abb}. Урон будет увеличен на {damage} единиц!");
    }

    public static void ChangeAfterDeffenceAbbility(Abbility abb, int damage, IPlayer player)
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"У {player} сработала способность! {abb}. Урон будет уменьшен на {damage} единиц!");
    }

    public static void ChangeAfterPassiveAbbility(PassiveAbbility abb, int score, IPlayer player)
    {
        WriteLineSeparator();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"У {player} активна пассивка! {abb}. {abb.Target} увеличена на {score} единиц!");
    }
}


