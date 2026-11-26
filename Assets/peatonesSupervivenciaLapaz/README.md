# Módulo de Peatones y NPCs (SanAndreasUnity)

Esta carpeta contiene los archivos clave extraídos del sistema de peatones y NPCs de **SanAndreasUnity**, organizados de manera modular para que puedas estudiarlos, adaptarlos o integrarlos en el desarrollo de tu propio juego.

---

## 📁 Estructura del Módulo

### 1. `Prefabs/`
* **`Ped.prefab`**: Prefab base que contiene la jerarquía completa del peatón (CharacterController, NavMeshAgent, MovementAgent, WeaponHolder, PlayerModel y el contenedor de Estados).
* **`PedMouthAudioSource.prefab`**: Fuente de audio anclada a la cabeza/boca para efectos vocales y sonidos.

### 2. `Core/` (Núcleo y Control Físico del Personaje)
* **`Ped.cs`**: Clase central que define a cualquier personaje humano (jugador o NPC). Maneja el input, inventario de armas, movimiento y referencias a componentes.
* **`Ped_Spawning.cs`**: Funciones estáticas y de utilidad para instanciar peatones (`SpawnPed`, `SpawnPedAI`, `SpawnPedStalker`).
* **`Ped_Damage.cs`**: Sistema de recepción de daño, multiplicadores por zona del cuerpo, barra de vida y activación de ragdoll.
* **`Ped_Vehicle.cs`**: Lógica de interacción y entrada/salida de vehículos (`EnterVehicle`, `ExitVehicle`).
* **`Ped_Sound.cs`**: Reproducción de efectos de pasos y sonidos contextuales.
* **`Ped_Networking.cs`**: Sincronización en red para servidores y clientes (Mirror).
* **`PedModel.cs`**: Controlador del esqueleto, animaciones esqueléticas, transformación de huesos y mezcla de animaciones (aiming/movement).
* **`PedManager.cs`**: Singleton configurable en Unity con ajustes de velocidad, distancias de IA, multiplicadores de daño y ajustes de ragdoll.
* **`PlayerController.cs`**: Mapeo de controles del teclado/ratón cuando el Ped es controlado por el jugador.
* **`DeadBody.cs`**: Manejo de cadáveres y tiempo de persistencia en la escena.
* **`States/`**: Todas las clases de la máquina de estados físico-animada:
  * *Locomoción:* `StandState.cs`, `WalkState.cs`, `RunState.cs`, `SprintState.cs`, `JumpState.cs`, `RollState.cs`, `CrouchState.cs`, `PanicState.cs`, `SurrenderState.cs`.
  * *Combate:* `StandAimState.cs`, `WalkAimState.cs`, `RunAimState.cs`, `StandFireState.cs`, `WalkFireState.cs`, etc.
  * *Vehículos:* `VehicleEnteringState.cs`, `VehicleSittingState.cs`, `VehicleExitingState.cs`, `DriveByState.cs`.

### 3. `AI/` (Inteligencia Artificial y Población)
* **`PedAI.cs`**: Cerebro del NPC. Contiene la máquina de estados de la IA, percepción sensorial (visión de cabeza mediante raycast, audición de disparos) y detección de amenazas.
* **`NPCPedSpawner.cs`**: Administrador de población. Maneja los límites de spawn/despawn según cuadrículas visibles (`WorldSystem`), distancias al jugador y asignación de armas según el tipo de NPC.
* **`WalkAroundState.cs`**: Comportamiento de navegación por aceras o nodos de caminos.
* **`ChaseState.cs`**: Lógica de combate y persecución (elección inteligente del mejor arma, cobertura y disparo).
* **`EscapeState.cs`**: Estado de huida en pánico cuando el NPC escucha armas o recibe daño.
* **`IdleState.cs`**: Estado inicial pasivo.
* **`FollowState.cs`**: Comportamiento de escolta/seguidor cuando un NPC es reclutado por el jugador o un líder.
* **`BaseState.cs`**, **`PathMovementData.cs`**, **`IPathMovementState.cs`**: Clases base y estructuras para la navegación sobre grafos de caminos.

### 4. `Importing_Definitions/` (Datos y Rutas)
* **`PedestrianDef.cs`**: Esquema de datos de peatones (IDs, tipos: civil, policía, bandas, banderas de comportamiento y animación).
* **`NodeFile.cs`**: Lógica de lectura y navegación sobre redes de nodos viales y peatonales (`NodeReader` y `PathNode`).

---

## 💡 Consejos para adaptar a tu propio juego

1. **Si usas modelos 3D modernos (FBX / Unity Humanoid):**
   * En SanAndreasUnity, `PedModel.cs` decodifica formatos nativos de GTA (`.dff` y `.ifp`).
   * Para tu juego, puedes sustituir `PedModel.cs` por un controlador estándar con `Animator` de Unity y un avatar mecanim Humanoid, reutilizando toda la lógica de `PedAI.cs` y los estados.
2. **Sistema de Rutas:**
   * La IA utiliza `MovementAgent` y Unity `NavMeshAgent` combinados con `PathNode`. Puedes reemplazar los nodos de GTA por puntos de paso propios (`Waypoints`) o únicamente la malla de navegación nativa de Unity (`NavMesh.CalculatePath`).
3. **Máquina de Estados:**
   * La separación entre el estado físico (`Ped/States`) y el estado de decisión (`PedAI`) te permite crear nuevos comportamientos (por ejemplo: policías que llaman refuerzos, vendedores ambulantes, etc.) simplemente creando nuevas clases que hereden de `BaseState` dentro de `AI/`.
