using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>
///Part -- еще один сериализуемый класс подобно WeaponDefinition, предназначенный для хранения данных
/// </summary>
[System.Serializable]
public class Part
{
    //Значения этих трех полей должны определяться в инспекторе
    public string name; //Имя этой части
    public float health; //ХП это части
    public string[] protectedBy; //Другие части, защищающие эту

    //Эти два поля инициализируются автоматически в Start().
    //Кэширование, как здесь, ускоряет получение необходимых данных
    [HideInInspector] //Не позволяет следующему полю появиться в инспекторе
    public GameObject go; //Игровой объект этой части
    public Material mat; //Материал для отображения повреждений
}

///<summary>
///Enemy_4 создается за верхней границей, выбирает случайную точку на экране и перемещается к ней.
///Добравшись до места, выбирает другую случайную точку и продолжает двигаться, пока игрок не уничтожит его.
///</summary>

public class Enemy_4 : Enemy {

    [Header("Set in Inspector: Enemy_4")]
    public Part[] parts; //Массив частей, состовляющих корабль

    private Vector3 p0, p1; //Две точки для интерполяции
    private float timeStart; //Время создания этого корабля
    private float duration = 4; //Продолжительность перемещения

    void Start()
    {
        //Начальная позиция уже выбрана в Main.SpawnEnemy(), поэтому запишем ее как начальные значения в p0 и p1
        p0 = p1 = pos;
        InitMovement();

        //Записать в кэш игровой объект и материал каждой части parts
        Transform t;
        foreach (Part prt in parts)
        {
            t = transform.Find(prt.name);
            if(t != null)
            {
                prt.go = t.gameObject;
                prt.mat = prt.go.GetComponent<Renderer>().material;
            }
        }
    }

    void InitMovement()
    {
        p0 = p1;  //Переписать р1 в р0
        //Выбрать новую точку р1 на экране
        float widMinRad = bndCheck.camWidth - bndCheck.radius;
        float hgtMinRad = bndCheck.camHeight - bndCheck.radius;
        p1.x = Random.Range(-widMinRad, widMinRad);
        p1.y = Random.Range(-hgtMinRad, hgtMinRad);

        //Сбросить время
        timeStart = Time.time;
    }

    public override void Move()
    {
        //Этот метод переопределяет Enemy.Move() и реализует линейную интерполяцию
        float u = (Time.time - timeStart) / duration;

        if (u >= 1)
        {
            InitMovement();
            u = 0;
        }

        u = 1 - Mathf.Pow(1 - u, 2); //Применить плавное замедление
        pos = (1 - u) * p0 + u * p1; //Простая линейная интерполяция
    }

    //Эти две функции осуществляют поиск части в parts n по имени или по ссылке на игрововй объект
    Part FindPart(string n)
    {
        foreach(Part prt in parts)
        {
            if(prt.name == n)
            {
                return (prt);
            }
        }
        return (null);
    }

    Part FindPart(GameObject go)
    {
        foreach(Part prt in parts)
        {
            if(prt.go == go)
            {
                return (prt);
            }
        }
        return (null);
    }
    //Эти функции возращают true, если данная часть уничтожена
    bool Destroyed(GameObject go)
    {
        return (Destroyed(FindPart(go)));
    }
    bool Destroyed(string n)
    {
        return (Destroyed(FindPart(n)));
    }
    bool Destroyed (Part prt)
    {
        if (prt == null) //Если ссылка на часть не была передана
        {
            return (true); //Вернуть true, т.е. ДА, была уничтожена
        }
        //Вернуть результат сравнения: prt.health <= 0
        //Если prt.health <= 0, вернуть true (да, была уничтожена)
        return (prt.health <= 0);
    }
    
    //Окрашивает в красный только одну часть, а не весь корабль
    void ShowLocalizedDamage(Material m)
    {
        m.color = Color.red;
        damageDoneTime = Time.time + showDamageDuration;
        showingDamage = true;

    }

    //Переопределяет метод OnCollisionEnter из сценария Enemy.cs
    void OnCollisionEnter (Collision coll)
    {
        GameObject other = coll.gameObject;
        switch (other.tag)
        {
            case "ProjectileHero":
                Projectile p = other.GetComponent<Projectile>();
                //Если корабль за границами экрана, не повреждать его
                if (!bndCheck.isOnScreen)
                {
                    Destroy(other);
                    break;
                }
                //Поразить вражеский корабль
                GameObject goHit = coll.contacts[0].thisCollider.gameObject;
                Part prtHit = FindPart(goHit);
                if(prtHit == null) //Если prtHit не найден
                {
                    goHit = coll.contacts[0].otherCollider.gameObject;
                    prtHit = FindPart(goHit);
                }
                //Проверить, защищена ли эта часть корабля
                if(prtHit.protectedBy != null)
                {
                    foreach(string s in prtHit.protectedBy)
                    {
                        //Если хотя бы из защищающих частей еще не разрушена...
                        if (!Destroyed(s))
                        {
                            //... не наносить ей повреждений
                            Destroy(other); //уничтожить снаряд ProjectileHero
                            return; //Выйти, не повреждая Enemy_4
                        }
                    }
                }
                //Если часть не защищена, нанести ей урон
                //Получить разрушающую силу Projectile.type и WEAP_DICT
                prtHit.health -= Main.GetWeaponDefinition(p.type).damageOnHit;
                //Показать эффект попадания в часть
                ShowLocalizedDamage(prtHit.mat);
                if(prtHit.health <= 0)
                {
                    //Вместо разрушения всего корабля, деактивировать уничтоженную часть
                    prtHit.go.SetActive(false);
                }
                //Проверить был ли корабль, полностью разрушен
                bool allDestroyed = true; //Предположить, что разрушен
                foreach (Part prt in parts)
                {
                    if (!Destroyed(prt)) //Если какая-то часть еще осталась...
                    {
                        allDestroyed = false; //Запиать false в allDestroyed
                        break; //и прекратить цикл forach
                    }
                }
                    if (allDestroyed) // Если корабль разрушен полностью 
                    {
                        Main.S.ShipDestroyed(this);
                        //Уничтожить этот объект Enemy
                        Destroy(this.gameObject);
                    }
                    Destroy(other); //уничтожить снаряд projectileHero
                    break;
        }
    }
}
