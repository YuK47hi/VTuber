#define _CRT_SECURE_NO_WARNINGS // Visual Studioの安全警告を無効化

#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <time.h>   // タイムスタンプ取得用

#define MAX_INPUT_SIZE 256
#define MAX_NODES 50      // ファイルとディレクトリの合計最大数
#define MAX_PROCESSES 10  // 同時に実行できるプロセスの最大数
#define MAX_RAM_MB 1024   // システムの総メモリ量 (MB)
#define MAX_VMS 5         // 作成できる仮想マシンの最大数
#define SAVE_FILE_PATH "E:\\OS\\myos_save.dat"

// --- 1. 仮想ファイルシステム ---
#define TYPE_FILE 0
#define TYPE_DIR  1

typedef struct {
    int in_use;
    int type;
    int parent_id;
    char name[32];
    char content[256];
    size_t size;        // ファイルサイズ（バイト数）
    time_t created_at;  // 作成日時
    char owner[32];     // 所有者ユーザー名
} FSNode;

FSNode fs[MAX_NODES];
int current_dir_id = 0;

// カレントユーザー管理（デフォルト: root）
char current_user[32] = "root";

int is_root() {
    return strcmp(current_user, "root") == 0;
}

void init_fs() {
    for (int i = 0; i < MAX_NODES; i++) {
        fs[i].in_use = 0;
    }
    // ルートディレクトリ
    fs[0].in_use = 1;
    fs[0].type = TYPE_DIR;
    fs[0].parent_id = -1;
    strcpy(fs[0].name, "root");
    fs[0].size = 0;
    fs[0].created_at = time(NULL);
    strcpy(fs[0].owner, "root");
}

void get_current_path(char* path_buffer) {
    char temp[MAX_NODES][32];
    int count = 0;
    int curr = current_dir_id;

    while (curr != -1) {
        strcpy(temp[count++], fs[curr].name);
        curr = fs[curr].parent_id;
    }

    path_buffer[0] = '\0';
    for (int i = count - 1; i >= 0; i--) {
        strcat(path_buffer, "/");
        if (strcmp(temp[i], "root") != 0) {
            strcat(path_buffer, temp[i]);
        }
    }
}

// --- 2. 仮想プロセスマネージャ & メモリ管理 ---
typedef struct {
    int in_use;
    int pid;
    int mem_usage;
    char name[32];
    char state[16];
    char owner[32];
} OSProcess;

OSProcess ptable[MAX_PROCESSES];
int next_pid = 100;
int used_ram = 0;

void init_ps() {
    for (int i = 0; i < MAX_PROCESSES; i++) {
        ptable[i].in_use = 0;
    }
    ptable[0].in_use = 1;
    ptable[0].pid = 1;
    ptable[0].mem_usage = 128;
    used_ram = 128;
    strcpy(ptable[0].name, "system_idle");
    strcpy(ptable[0].state, "Running");
    strcpy(ptable[0].owner, "root");
}

// --- 3. 仮想マシン (VM) 管理機能 ---
typedef struct {
    int in_use;
    char name[32];
    int ram_mb;
    int disk_mb;
    char state[16];     // "Stopped" または "Running"
    char owner[32];
    int pid;            // 実行中のPID (停止時は -1)
} VirtualMachine;

VirtualMachine vm_list[MAX_VMS];

void init_vm() {
    for (int i = 0; i < MAX_VMS; i++) {
        vm_list[i].in_use = 0;
        vm_list[i].pid = -1;
    }
}

// --- 別ウィンドウ（ウィンドウモード）でVMを起動する関数 ---
void launch_vm_window(const char* vm_name) {
    char win_cmd[1024];

    /*
       【オプション】PCに本物の Ubuntu (WSL) が導入されている場合は
       下のコメントアウト（//）を外して実行すると本物のUbuntu画面が開きます。

       snprintf(win_cmd, sizeof(win_cmd), "start \"VM: %s\" wsl", vm_name);
    */

    // 疑似Ubuntuの起動画面（別ウィンドウで起動）
    snprintf(win_cmd, sizeof(win_cmd),
        "start \"VM: %s\" cmd /k \""
        "title VM: %s (Ubuntu 24.04 LTS) && color 0A && cls && "
        "echo [  OK  ] Starting systemd-udevd.service... && "
        "echo [  OK  ] Started Thermal Daemon Service. && "
        "echo [  OK  ] Reached target System Initialization. && "
        "echo [  OK  ] Started Virtual Machine Container. && "
        "echo. && "
        "echo Welcome to Ubuntu 24.04 LTS (GNU/Linux x86_64) && "
        "echo * Documentation:  https://help.ubuntu.com && "
        "echo * Management:     https://landscape.canonical.com && "
        "echo. && "
        "echo Type 'exit' to close this VM window. && "
        "echo. && "
        "prompt %s@ubuntu-vm:~$ \"",
        vm_name, vm_name, vm_name);

    system(win_cmd);
}

// --- 4. セーブ＆ロード機能 ---
void save_fs() {
    system("mkdir E:\\OS > nul 2>&1");

    FILE* fp = fopen(SAVE_FILE_PATH, "wb");
    if (fp != NULL) {
        fwrite(fs, sizeof(FSNode), MAX_NODES, fp);
        fwrite(vm_list, sizeof(VirtualMachine), MAX_VMS, fp);
        fclose(fp);
        printf("  システム状態（FS + VMデータ）を '%s' にセーブしました。\n", SAVE_FILE_PATH);
    }
    else {
        printf("  エラー: セーブデータの保存に失敗しました。\n");
    }
}

int load_fs() {
    FILE* fp = fopen(SAVE_FILE_PATH, "rb");
    if (fp != NULL) {
        fread(fs, sizeof(FSNode), MAX_NODES, fp);
        fread(vm_list, sizeof(VirtualMachine), MAX_VMS, fp);
        fclose(fp);

        // 起動時はすべて停止状態として復元
        for (int i = 0; i < MAX_VMS; i++) {
            if (vm_list[i].in_use) {
                strcpy(vm_list[i].state, "Stopped");
                vm_list[i].pid = -1;
            }
        }
        return 1;
    }
    else {
        init_fs();
        init_vm();
        return 0;
    }
}

void clear_screen() {
    system("cls");
}

int main() {
    srand((unsigned int)time(NULL));
    char input[MAX_INPUT_SIZE];
    char prompt_buffer[256];

    int loaded = load_fs();
    init_ps();

    clear_screen();
    printf("====================================================\n");
    printf("  My Custom OS - VM Hypervisor Edition\n");
    printf("  Save Path: %s\n", SAVE_FILE_PATH);
    printf("  Total RAM: %d MB\n", MAX_RAM_MB);
    printf("  Commands: help, mkvm, lsvm, startvm, stopvm, rmvm\n");
    printf("====================================================\n");
    if (loaded) {
        printf("  [Info] セーブデータを読み込みました！\n\n");
    }
    else {
        printf("  [Info] 新規ファイルシステムを構築しました。\n\n");
    }

    while (1) {
        get_current_path(prompt_buffer);
        printf("%s@MyOS:%s%s ", current_user, prompt_buffer, is_root() ? "#" : "$");

        if (fgets(input, MAX_INPUT_SIZE, stdin) == NULL) break;
        input[strcspn(input, "\n")] = '\0';
        if (strlen(input) == 0) continue;

        char cmd[32] = { 0 }, arg1[32] = { 0 }, arg2[256] = { 0 };
        int parsed = sscanf(input, "%31s %31s %255[^\n]", cmd, arg1, arg2);

        // --- コマンド処理 ---
        if (strcmp(cmd, "help") == 0) {
            printf("\n  [仮想マシン (VM) 管理]\n");
            printf("  mkvm [name] [RAM_MB] [DISK_MB] : 仮想マシンを作成\n");
            printf("  lsvm                           : VM一覧とステータス表示\n");
            printf("  startvm [name]                 : VMをウィンドウモードで起動\n");
            printf("  stopvm [name]                  : VMを停止\n");
            printf("  rmvm [name]                    : VMを削除\n");
            printf("\n  [ユーザー管理]\n");
            printf("  su [user], whoami\n");
            printf("\n  [ファイルシステム]\n");
            printf("  ls, ls -l, mkdir, cd, touch, write, cat, rm\n");
            printf("\n  [システム]\n");
            printf("  run, ps, kill, free, save, clear, exit\n\n");
        }
        // --- VM管理コマンド ---
        else if (strcmp(cmd, "mkvm") == 0) {
            char vm_name[32] = { 0 };
            int ram = 0, disk = 0;
            int args_count = sscanf(input, "%*s %31s %d %d", vm_name, &ram, &disk);

            if (args_count < 3 || ram <= 0 || disk <= 0) {
                printf("  使い方: mkvm [VM名] [RAM容量(MB)] [DISK容量(MB)]\n");
                printf("  例: mkvm ubuntu 256 1024\n");
            }
            else {
                int created = 0;
                for (int i = 0; i < MAX_VMS; i++) {
                    if (!vm_list[i].in_use) {
                        vm_list[i].in_use = 1;
                        strcpy(vm_list[i].name, vm_name);
                        vm_list[i].ram_mb = ram;
                        vm_list[i].disk_mb = disk;
                        strcpy(vm_list[i].state, "Stopped");
                        strcpy(vm_list[i].owner, current_user);
                        vm_list[i].pid = -1;
                        printf("  仮想マシン '%s' (RAM: %dMB, DISK: %dMB) を作成しました！\n", vm_name, ram, disk);
                        created = 1;
                        break;
                    }
                }
                if (!created) printf("  エラー: 作成可能なVMの最大数 (%d台) に達しています。\n", MAX_VMS);
            }
        }
        else if (strcmp(cmd, "lsvm") == 0) {
            printf("  VM NAME          OWNER        RAM(MB)   DISK(MB)  STATUS      PID\n");
            printf("  -------------------------------------------------------------------\n");
            int count = 0;
            for (int i = 0; i < MAX_VMS; i++) {
                if (vm_list[i].in_use) {
                    char pid_str[16];
                    if (vm_list[i].pid != -1) sprintf(pid_str, "%d", vm_list[i].pid);
                    else strcpy(pid_str, "-");

                    printf("  %-16s %-12s %-9d %-9d %-10s %s\n",
                        vm_list[i].name, vm_list[i].owner, vm_list[i].ram_mb,
                        vm_list[i].disk_mb, vm_list[i].state, pid_str);
                    count++;
                }
            }
            if (count == 0) printf("  (作成された仮想マシンはありません)\n");
            printf("  -------------------------------------------------------------------\n");
        }
        else if (strcmp(cmd, "startvm") == 0 && parsed >= 2) {
            int found = 0;
            for (int i = 0; i < MAX_VMS; i++) {
                if (vm_list[i].in_use && strcmp(vm_list[i].name, arg1) == 0) {
                    found = 1;
                    if (strcmp(vm_list[i].state, "Running") == 0) {
                        printf("  エラー: 仮想マシン '%s' はすでに起動しています。\n", arg1);
                        break;
                    }
                    if (used_ram + vm_list[i].ram_mb > MAX_RAM_MB) {
                        printf("  エラー: メモリ不足のためVMを起動できません。(空き: %d MB)\n", MAX_RAM_MB - used_ram);
                        break;
                    }
                    // プロセステーブルにVMプロセスを登録
                    int started_ps = 0;
                    for (int p = 0; p < MAX_PROCESSES; p++) {
                        if (!ptable[p].in_use) {
                            ptable[p].in_use = 1;
                            ptable[p].pid = next_pid++;
                            ptable[p].mem_usage = vm_list[i].ram_mb;
                            snprintf(ptable[p].name, sizeof(ptable[p].name), "VM:%s", vm_list[i].name);
                            strcpy(ptable[p].state, "Running");
                            strcpy(ptable[p].owner, vm_list[i].owner);
                            used_ram += vm_list[i].ram_mb;

                            // VM側の状態更新
                            strcpy(vm_list[i].state, "Running");
                            vm_list[i].pid = ptable[p].pid;

                            printf("  [起動] 仮想マシン '%s' を別ウィンドウで起動しました！ (PID: %d)\n",
                                vm_list[i].name, ptable[p].pid);

                            // ★ここで別ウィンドウをポップアップ表示
                            launch_vm_window(vm_list[i].name);

                            started_ps = 1;
                            break;
                        }
                    }
                    if (!started_ps) printf("  エラー: プロセステーブルがいっぱいです。\n");
                    break;
                }
            }
            if (!found) printf("  エラー: 仮想マシン '%s' が見つかりません。\n", arg1);
        }
        else if (strcmp(cmd, "stopvm") == 0 && parsed >= 2) {
            int found = 0;
            for (int i = 0; i < MAX_VMS; i++) {
                if (vm_list[i].in_use && strcmp(vm_list[i].name, arg1) == 0) {
                    found = 1;
                    if (strcmp(vm_list[i].state, "Stopped") == 0) {
                        printf("  エラー: 仮想マシン '%s' は停止しています。\n", arg1);
                        break;
                    }
                    if (!is_root() && strcmp(vm_list[i].owner, current_user) != 0) {
                        printf("  エラー: 他のユーザー(%s)のVMを停止する権限がありません。\n", vm_list[i].owner);
                        break;
                    }

                    for (int p = 0; p < MAX_PROCESSES; p++) {
                        if (ptable[p].in_use && ptable[p].pid == vm_list[i].pid) {
                            ptable[p].in_use = 0;
                            used_ram -= ptable[p].mem_usage;
                            break;
                        }
                    }

                    strcpy(vm_list[i].state, "Stopped");
                    vm_list[i].pid = -1;
                    printf("  仮想マシン '%s' を停止しました。(メモリ解放)\n", arg1);
                    break;
                }
            }
            if (!found) printf("  エラー: 仮想マシン '%s' が見つかりません。\n", arg1);
        }
        else if (strcmp(cmd, "rmvm") == 0 && parsed >= 2) {
            int found = 0;
            for (int i = 0; i < MAX_VMS; i++) {
                if (vm_list[i].in_use && strcmp(vm_list[i].name, arg1) == 0) {
                    found = 1;
                    if (!is_root() && strcmp(vm_list[i].owner, current_user) != 0) {
                        printf("  エラー: 他のユーザー(%s)のVMを削除する権限がありません。\n", vm_list[i].owner);
                        break;
                    }
                    if (strcmp(vm_list[i].state, "Running") == 0) {
                        printf("  エラー: 実行中のVMは削除できません。\n");
                        break;
                    }
                    vm_list[i].in_use = 0;
                    printf("  仮想マシン '%s' を削除しました。\n", arg1);
                    break;
                }
            }
            if (!found) printf("  エラー: 仮想マシン '%s' が見つかりません。\n", arg1);
        }
        // --- ユーザー管理 ---
        else if (strcmp(cmd, "whoami") == 0) {
            printf("  %s\n", current_user);
        }
        else if (strcmp(cmd, "su") == 0 || strcmp(cmd, "login") == 0) {
            if (parsed < 2) strcpy(current_user, "root");
            else strcpy(current_user, arg1);
            printf("  ユーザー '%s' としてログインしました。\n", current_user);
        }
        else if (strcmp(cmd, "save") == 0) {
            save_fs();
        }
        // --- ファイルシステム ---
        else if (strcmp(cmd, "ls") == 0) {
            int is_detailed = (parsed >= 2 && strcmp(arg1, "-l") == 0);
            int count = 0;

            if (is_detailed) {
                printf("  TYPE    OWNER        SIZE (Bytes)  CREATED AT           NAME\n");
                printf("  -------------------------------------------------------------------\n");
            }

            for (int i = 0; i < MAX_NODES; i++) {
                if (fs[i].in_use && fs[i].parent_id == current_dir_id) {
                    if (is_detailed) {
                        char time_buf[32];
                        struct tm* timeinfo = localtime(&fs[i].created_at);
                        strftime(time_buf, sizeof(time_buf), "%Y-%m-%d %H:%M:%S", timeinfo);

                        if (fs[i].type == TYPE_DIR) printf("  <DIR>   %-11s  %-12s  %s  %s\n", fs[i].owner, "-", time_buf, fs[i].name);
                        else printf("  <FILE>  %-11s  %-12zu  %s  %s\n", fs[i].owner, fs[i].size, time_buf, fs[i].name);
                    }
                    else {
                        if (fs[i].type == TYPE_DIR) printf("  <DIR>  %s\n", fs[i].name);
                        else printf("         %s\n", fs[i].name);
                    }
                    count++;
                }
            }
            if (count == 0) printf("  (空です)\n");
        }
        else if (strcmp(cmd, "mkdir") == 0 && parsed >= 2) {
            for (int i = 0; i < MAX_NODES; i++) {
                if (!fs[i].in_use) {
                    fs[i].in_use = 1;
                    fs[i].type = TYPE_DIR;
                    fs[i].parent_id = current_dir_id;
                    strcpy(fs[i].name, arg1);
                    fs[i].size = 0;
                    fs[i].created_at = time(NULL);
                    strcpy(fs[i].owner, current_user);
                    printf("  ディレクトリ '%s' を作成しました。\n", arg1);
                    break;
                }
            }
        }
        else if (strcmp(cmd, "touch") == 0 && parsed >= 2) {
            for (int i = 0; i < MAX_NODES; i++) {
                if (!fs[i].in_use) {
                    fs[i].in_use = 1;
                    fs[i].type = TYPE_FILE;
                    fs[i].parent_id = current_dir_id;
                    strcpy(fs[i].name, arg1);
                    fs[i].content[0] = '\0';
                    fs[i].size = 0;
                    fs[i].created_at = time(NULL);
                    strcpy(fs[i].owner, current_user);
                    printf("  ファイル '%s' を作成しました。\n", arg1);
                    break;
                }
            }
        }
        else if (strcmp(cmd, "write") == 0 && parsed >= 3) {
            int found = 0;
            for (int i = 0; i < MAX_NODES; i++) {
                if (fs[i].in_use && fs[i].type == TYPE_FILE &&
                    fs[i].parent_id == current_dir_id && strcmp(fs[i].name, arg1) == 0) {
                    if (!is_root() && strcmp(fs[i].owner, current_user) != 0) {
                        printf("  エラー: パーミッションがありません。(所有者: %s)\n", fs[i].owner);
                        found = 1;
                        break;
                    }
                    strcpy(fs[i].content, arg2);
                    fs[i].size = strlen(arg2);
                    printf("  '%s' に書き込みました。\n", arg1);
                    found = 1;
                    break;
                }
            }
            if (!found) printf("  エラー: '%s' が見つかりません。\n", arg1);
        }
        else if (strcmp(cmd, "cd") == 0 && parsed >= 2) {
            if (strcmp(arg1, "..") == 0) {
                if (fs[current_dir_id].parent_id != -1) current_dir_id = fs[current_dir_id].parent_id;
                else printf("  これ以上上の階層はありません。\n");
            }
            else {
                int found = 0;
                for (int i = 0; i < MAX_NODES; i++) {
                    if (fs[i].in_use && fs[i].type == TYPE_DIR &&
                        fs[i].parent_id == current_dir_id && strcmp(fs[i].name, arg1) == 0) {
                        current_dir_id = i;
                        found = 1;
                        break;
                    }
                }
                if (!found) printf("  エラー: '%s' が見つかりません。\n", arg1);
            }
        }
        else if (strcmp(cmd, "cat") == 0 && parsed >= 2) {
            int found = 0;
            for (int i = 0; i < MAX_NODES; i++) {
                if (fs[i].in_use && fs[i].type == TYPE_FILE &&
                    fs[i].parent_id == current_dir_id && strcmp(fs[i].name, arg1) == 0) {
                    printf("  %s\n", fs[i].content);
                    found = 1;
                    break;
                }
            }
            if (!found) printf("  エラー: '%s' が見つかりません。\n", arg1);
        }
        else if (strcmp(cmd, "rm") == 0 && parsed >= 2) {
            int found = 0;
            for (int i = 0; i < MAX_NODES; i++) {
                if (fs[i].in_use && fs[i].parent_id == current_dir_id && strcmp(fs[i].name, arg1) == 0) {
                    if (!is_root() && strcmp(fs[i].owner, current_user) != 0) {
                        printf("  エラー: パーミッションがありません。(所有者: %s)\n", fs[i].owner);
                        found = 1;
                        break;
                    }
                    fs[i].in_use = 0;
                    printf("  '%s' を削除しました。\n", arg1);
                    found = 1;
                    break;
                }
            }
            if (!found) printf("  エラー: '%s' が見つかりません。\n", arg1);
        }
        // --- システム・プロセス ---
        else if (strcmp(cmd, "run") == 0 && parsed >= 2) {
            int req_mem = (rand() % 21 + 5) * 10;
            if (used_ram + req_mem > MAX_RAM_MB) {
                printf("  エラー: メモリ不足です！ (空き: %d MB)\n", MAX_RAM_MB - used_ram);
            }
            else {
                int started = 0;
                for (int i = 0; i < MAX_PROCESSES; i++) {
                    if (!ptable[i].in_use) {
                        ptable[i].in_use = 1;
                        ptable[i].pid = next_pid++;
                        ptable[i].mem_usage = req_mem;
                        strcpy(ptable[i].name, arg1);
                        strcpy(ptable[i].state, "Running");
                        strcpy(ptable[i].owner, current_user);
                        used_ram += req_mem;
                        printf("  プロセス '%s' (PID: %d) を開始。(メモリ: %d MB)\n", arg1, ptable[i].pid, req_mem);
                        started = 1;
                        break;
                    }
                }
                if (!started) printf("  エラー: プロセステーブルがいっぱいです。\n");
            }
        }
        else if (strcmp(cmd, "ps") == 0) {
            printf("  PID   OWNER        MEM(MB)  STATE      NAME\n");
            printf("  ---------------------------------------------------\n");
            for (int i = 0; i < MAX_PROCESSES; i++) {
                if (ptable[i].in_use) {
                    printf("  %-5d %-12s %-8d %-10s %s\n", ptable[i].pid, ptable[i].owner, ptable[i].mem_usage, ptable[i].state, ptable[i].name);
                }
            }
            printf("  ---------------------------------------------------\n");
        }
        else if (strcmp(cmd, "kill") == 0 && parsed >= 2) {
            int target_pid = atoi(arg1);
            if (target_pid == 1) {
                printf("  エラー: システムプロセスは終了できません。\n");
                continue;
            }
            int found = 0;
            for (int i = 0; i < MAX_PROCESSES; i++) {
                if (ptable[i].in_use && ptable[i].pid == target_pid) {
                    if (!is_root() && strcmp(ptable[i].owner, current_user) != 0) {
                        printf("  エラー: 権限がありません。(所有者: %s)\n", ptable[i].owner);
                        found = 1;
                        break;
                    }
                    ptable[i].in_use = 0;
                    used_ram -= ptable[i].mem_usage;

                    for (int v = 0; v < MAX_VMS; v++) {
                        if (vm_list[v].in_use && vm_list[v].pid == target_pid) {
                            strcpy(vm_list[v].state, "Stopped");
                            vm_list[v].pid = -1;
                            break;
                        }
                    }

                    printf("  プロセス '%s' をキルしました。(メモリ %d MB 解放)\n", ptable[i].name, ptable[i].mem_usage);
                    found = 1;
                    break;
                }
            }
            if (!found) printf("  エラー: PID %d が存在しません。\n", target_pid);
        }
        else if (strcmp(cmd, "free") == 0) {
            printf("  Total RAM: %4d MB\n  Used RAM : %4d MB\n  Free RAM : %4d MB\n", MAX_RAM_MB, used_ram, MAX_RAM_MB - used_ram);
        }
        else if (strcmp(cmd, "exit") == 0) {
            break;
        }
        else if (strcmp(cmd, "clear") == 0) {
            clear_screen();
        }
        else {
            printf("不明なコマンドか、引数が足りません: '%s'\n", input);
        }
    }
    return 0;
}