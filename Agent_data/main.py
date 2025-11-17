import numpy as np
import matplotlib.pyplot as plt
import random
import math

#Параметры мира
MAP_SIZE = 60
NUM_OBSTACLES = 120
SENSOR_RANGE = 12
STEP_LIMIT = 800
NUM_LIDAR_RAYS = 36  #количество лучей (360 / 36 = 10 шаг)

#Создание мира
world = np.zeros((MAP_SIZE, MAP_SIZE), dtype=int)
for _ in range(NUM_OBSTACLES):
    x, y = random.randint(0, MAP_SIZE-1), random.randint(0, MAP_SIZE-1)
    world[y, x] = 1  #1 = препятствие

#Робот
robot_pos = np.array([MAP_SIZE//2, MAP_SIZE//2], dtype=float)
robot_angle = 0.0  #направление (0 = вверх)
map_estimate = np.full_like(world, -1)  #-1 = неизвестно

#Движение
def move_forward(pos, angle):
    dx = int(round(math.cos(math.radians(angle))))
    dy = int(round(math.sin(math.radians(angle))))
    return pos + np.array([dx, dy])

def turn_left(angle, deg=30):
    return (angle + deg) % 360

def turn_right(angle, deg=30):
    return (angle - deg) % 360

#Лидар
def lidar_scan(pos, angle, num_rays=NUM_LIDAR_RAYS):
    hits = []
    rays = []  #для визуализации
    for i in range(num_rays):
        ray_angle = math.radians(angle + i * (360 / num_rays))
        last_x, last_y = None, None
        for r in np.linspace(0, SENSOR_RANGE, SENSOR_RANGE * 5):
            x = int(round(pos[0] + math.cos(ray_angle) * r))
            y = int(round(pos[1] + math.sin(ray_angle) * r))
            if 0 <= x < MAP_SIZE and 0 <= y < MAP_SIZE:
                last_x, last_y = x, y
                if world[y, x] == 1:
                    hits.append((x, y, 1))
                    break
                else:
                    hits.append((x, y, 0))
            else:
                break
        if last_x is not None:
            rays.append(((pos[0], pos[1]), (last_x, last_y)))
    return hits, rays

#Визуализация
plt.ion()
fig, ax = plt.subplots(figsize=(7,7))

def draw(world, robot, est, rays, step):
    color_map = np.zeros((MAP_SIZE, MAP_SIZE, 3))
    for y in range(MAP_SIZE):
        for x in range(MAP_SIZE):
            val = est[y, x]
            if val == -1:
                color_map[y, x] = [0.5, 0.5, 0.5]  #неизвестно
            elif val == 0:
                color_map[y, x] = [0.0, 0.8, 0.0]  #свободно
            elif val == 1:
                color_map[y, x] = [0.0, 0.0, 0.0]  #стена

    ax.clear()
    ax.imshow(color_map, origin="lower")

    #Рисуем лучи лидара
    for (x1, y1), (x2, y2) in rays:
        ax.plot([x1, x2], [y1, y2], color=(1, 1, 0, 0.4), linewidth=0.7)  #жёлтые лучи

    #Робот (красный кружок)
    ax.scatter(robot[0], robot[1], color='red', s=60, edgecolors='black')

    # Направление взгляда
    view_x = robot[0] + math.cos(math.radians(robot_angle)) * 2
    view_y = robot[1] + math.sin(math.radians(robot_angle)) * 2
    ax.plot([robot[0], view_x], [robot[1], view_y], color='orange', linewidth=2)

    ax.set_title(f"SLAM: визуализация лидара (шаг {step})")
    ax.axis("off")
    plt.pause(0.001)

#Основной цикл
for step in range(STEP_LIMIT):
    # Сканирование лидара
    scan, rays = lidar_scan(robot_pos, robot_angle)
    for x, y, val in scan:
        map_estimate[y, x] = val

    # Рисуем
    draw(world, robot_pos, map_estimate, rays, step)

    # Проверяем, есть ли стена впереди
    front = move_forward(robot_pos, robot_angle)
    fx, fy = int(round(front[0])), int(round(front[1]))
    blocked = (
        fx < 0 or fy < 0 or fx >= MAP_SIZE or fy >= MAP_SIZE or world[fy, fx] == 1
    )

    if blocked:
        if random.random() < 0.5:
            robot_angle = turn_left(robot_angle)
        else:
            robot_angle = turn_right(robot_angle)
    else:
        robot_pos = front

plt.ioff()
plt.show()
