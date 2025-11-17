import numpy as np
import matplotlib.pyplot as plt
import random
import math

# --- Параметры мира ---
MAP_SIZE = 60
NUM_OBSTACLES = 120
SENSOR_RANGE = 12
STEP_LIMIT = 800

# --- Создание карты ---
world = np.zeros((MAP_SIZE, MAP_SIZE), dtype=int)
for _ in range(NUM_OBSTACLES):
    x, y = random.randint(0, MAP_SIZE-1), random.randint(0, MAP_SIZE-1)
    world[y, x] = 1  # 1 = препятствие

# --- Параметры робота ---
robot_pos = np.array([MAP_SIZE//2, MAP_SIZE//2], dtype=float)
robot_angle = 0.0  # направление в градусах (0° = вверх)
map_estimate = np.full_like(world, -1)  # -1 = неизвестно

# --- Функции движения робота ---
def move_forward(pos, angle):
    dx = int(round(math.cos(math.radians(angle))))
    dy = int(round(math.sin(math.radians(angle))))
    new_pos = pos + np.array([dx, dy])
    return new_pos

def turn_left(angle, degrees=30):
    return (angle + degrees) % 360

def turn_right(angle, degrees=30):
    return (angle - degrees) % 360

# --- Лидар (реалистичный) ---
def lidar_scan(pos, angle, num_rays=36):
    hits = []
    for i in range(num_rays):
        ray_angle = math.radians(angle + i * (360 / num_rays))
        for r in np.linspace(0, SENSOR_RANGE, SENSOR_RANGE * 5):
            x = int(round(pos[0] + math.cos(ray_angle) * r))
            y = int(round(pos[1] + math.sin(ray_angle) * r))
            if 0 <= x < MAP_SIZE and 0 <= y < MAP_SIZE:
                if world[y, x] == 1:
                    hits.append((x, y, 1))
                    break
                else:
                    hits.append((x, y, 0))
            else:
                break
    return hits

# --- Визуализация ---
plt.ion()
fig, ax = plt.subplots(figsize=(7,7))

def draw(world, robot, est, step):
    color_map = np.zeros((MAP_SIZE, MAP_SIZE, 3))
    for y in range(MAP_SIZE):
        for x in range(MAP_SIZE):
            val = est[y, x]
            if val == -1:
                color_map[y, x] = [0.5, 0.5, 0.5]  # неизвестно
            elif val == 0:
                color_map[y, x] = [0.0, 0.8, 0.0]  # свободно
            elif val == 1:
                color_map[y, x] = [0.0, 0.0, 0.0]  # стена
    # рисуем робота
    rx, ry = int(robot[0]), int(robot[1])
    if 0 <= rx < MAP_SIZE and 0 <= ry < MAP_SIZE:
        color_map[ry, rx] = [1.0, 0.0, 0.0]  # робот (красный)
    ax.clear()
    ax.imshow(color_map, origin="lower")
    ax.set_title(f"SLAM: реалистичное исследование (шаг {step})")
    ax.axis("off")
    plt.pause(0.001)

# --- Основной цикл ---
for step in range(STEP_LIMIT):
    # Лидар обновляет карту
    scan = lidar_scan(robot_pos, robot_angle)
    for x, y, val in scan:
        map_estimate[y, x] = val

    # Визуализация
    draw(world, robot_pos, map_estimate, step)

    # --- Выбор стратегии движения ---
    # Если впереди стена — поворачиваем
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
        # Двигаемся вперёд
        robot_pos = front

plt.ioff()
plt.show()
