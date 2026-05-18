/* ========================================
   EfiGuard UI - Renderer (i18n + Particles + Animations)
   ======================================== */

// =====================
// 国际化 i18n
// =====================
const i18nData = {
  zh: {
    nav_dashboard: '仪表盘',
    nav_operations: '操作',
    nav_logs: '日志',
    live_monitoring: '实时监控中',
    hero_title: '系统安全中心',
    hero_subtitle: '可视化管理系统虚拟化与安全功能状态',
    os_info: '系统信息',
    last_updated: '最后更新',
    refresh_interval: '刷新间隔',
    system_ops: '系统操作',
    ops_title: '操作中心',
    ops_subtitle: '管理 VBS 与 EfiGuard 启动配置',
    disable_vbs_title: '关闭 VBS',
    tag_danger: '高风险',
    disable_vbs_desc: '关闭基于虚拟化的安全功能和所有 Hyper-V 相关组件。将修改 Windows 可选功能和 BCD 配置，重启后生效。',
    btn_disable_vbs: '立即关闭 VBS',
    install_efiguard_title: '安装 EfiGuard',
    tag_advanced: '高级',
    install_efiguard_desc: '将预打包的 EfiGuard v1.3 部署到 EFI 系统分区并创建 BCD 启动项。EfiGuard 会在启动时禁用 PatchGuard 和驱动签名强制 (DSE)。',
    btn_install_efi: '一键安装到 ESP',
    system_logs: '系统日志',
    logs_title: '操作日志',
    logs_subtitle: '命令执行输出与状态记录',
    logs_live: '实时输出',
    clear_logs: '清空',
    logs_placeholder: '等待操作执行...',
    admin_warning: '未以管理员身份运行。所有操作按钮已禁用。',
    warn_title: '重要提示',
    warn_hvci: 'EfiGuard 无法禁用 HVCI（内存完整性）。如果 HVCI 处于启用状态，EfiGuard 的 DSE 绕过将无效。',
    warn_reboot: '所有系统级修改都需要重启计算机才能生效。',
    status_on: '启用',
    status_off: '禁用',
    status_warn: '警告',
    status_unknown: '未知',
    status_pending: '待处理',
    confirm_disable_vbs_title: '关闭 VBS',
    confirm_disable_vbs_msg: '即将关闭 Virtualization-based Security 及相关 Hyper-V 功能。',
    confirm_disable_vbs_detail: '计算机需要重启才能使更改生效。是否继续？',
    confirm_install_efi_title: '安装 EfiGuard',
    confirm_install_efi_msg: '即将修改 EFI 系统分区和 BCD 启动配置。',
    confirm_install_efi_detail: '将创建新的启动项 "EfiGuard Loader"。是否继续？',
    toast_vbs_done: 'VBS 关闭命令已执行',
    toast_vbs_reboot: '请重启计算机使更改生效',
    toast_efi_done: 'EfiGuard 安装完成',
    toast_efi_reboot: '重启后选择 "EfiGuard Loader" 启动',
    efi_bundled_ok: '✓ EfiGuard v1.3 已集成',
    efi_bundled_missing: '✗ 未找到本地 EfiGuard 文件',
  },
  en: {
    nav_dashboard: 'Dashboard',
    nav_operations: 'Operations',
    nav_logs: 'Logs',
    live_monitoring: 'Live Monitoring',
    hero_title: 'Security Center',
    hero_subtitle: 'Visualize system virtualization and security features',
    os_info: 'OS Info',
    last_updated: 'Last Updated',
    refresh_interval: 'Interval',
    system_ops: 'System Ops',
    ops_title: 'Operations',
    ops_subtitle: 'Manage VBS and EfiGuard boot configuration',
    disable_vbs_title: 'Disable VBS',
    tag_danger: 'DANGER',
    disable_vbs_desc: 'Turn off Virtualization-based Security and all Hyper-V components. Will modify Windows Optional Features and BCD config. Reboot required.',
    btn_disable_vbs: 'Disable VBS Now',
    install_efiguard_title: 'Install EfiGuard',
    tag_advanced: 'ADVANCED',
    install_efiguard_desc: 'Deploy bundled EfiGuard v1.3 to the EFI System Partition and create a BCD boot entry. EfiGuard disables PatchGuard and Driver Signature Enforcement (DSE) at boot.',
    btn_install_efi: 'Install to ESP',
    system_logs: 'System Logs',
    logs_title: 'Operation Logs',
    logs_subtitle: 'Command output and status records',
    logs_live: 'Live Output',
    clear_logs: 'Clear',
    logs_placeholder: 'Waiting for operations...',
    admin_warning: 'Not running as Administrator. All operations are disabled.',
    warn_title: 'Important Notice',
    warn_hvci: 'EfiGuard cannot disable HVCI (Memory Integrity). If HVCI is enabled, EfiGuard DSE bypass will be ineffective.',
    warn_reboot: 'All system-level changes require a reboot to take effect.',
    status_on: 'ON',
    status_off: 'OFF',
    status_warn: 'WARN',
    status_unknown: 'UNKNOWN',
    status_pending: 'PENDING',
    confirm_disable_vbs_title: 'Disable VBS',
    confirm_disable_vbs_msg: 'About to disable Virtualization-based Security and related Hyper-V features.',
    confirm_disable_vbs_detail: 'A reboot is required for changes to take effect. Continue?',
    confirm_install_efi_title: 'Install EfiGuard',
    confirm_install_efi_msg: 'About to modify the EFI System Partition and BCD store.',
    confirm_install_efi_detail: 'A new boot entry "EfiGuard Loader" will be created. Continue?',
    toast_vbs_done: 'VBS disable commands executed',
    toast_vbs_reboot: 'Please reboot your computer',
    toast_efi_done: 'EfiGuard installation complete',
    toast_efi_reboot: 'Select "EfiGuard Loader" at boot',
    efi_bundled_ok: '✓ EfiGuard v1.3 bundled',
    efi_bundled_missing: '✗ Bundled EfiGuard files not found',
  }
};

let currentLang = 'zh';

function t(key) {
  return i18nData[currentLang][key] || i18nData.en[key] || key;
}

function updateLanguage() {
  document.querySelectorAll('[data-i18n]').forEach(el => {
    const key = el.dataset.i18n;
    el.textContent = t(key);
  });
}

function toggleLanguage() {
  currentLang = currentLang === 'zh' ? 'en' : 'zh';
  updateLanguage();
  renderStatusCards(lastStatus);
  document.getElementById('langBtn').textContent = currentLang === 'zh' ? '中/EN' : 'EN/中';
}

// =====================
// 窗口控制
// =====================
document.getElementById('minBtn').addEventListener('click', () => window.electronAPI.minimizeWindow());
document.getElementById('maxBtn').addEventListener('click', () => window.electronAPI.maximizeWindow());
document.getElementById('closeBtn').addEventListener('click', () => window.electronAPI.closeWindow());
document.getElementById('langBtn').addEventListener('click', toggleLanguage);

// =====================
// 粒子背景
// =====================
(function initParticles() {
  const canvas = document.getElementById('particle-canvas');
  const ctx = canvas.getContext('2d');
  let particles = [];
  let w, h;

  function resize() {
    w = canvas.width = window.innerWidth;
    h = canvas.height = window.innerHeight;
  }
  resize();
  window.addEventListener('resize', resize);

  class Particle {
    constructor() {
      this.reset();
    }
    reset() {
      this.x = Math.random() * w;
      this.y = Math.random() * h;
      this.vx = (Math.random() - 0.5) * 0.3;
      this.vy = (Math.random() - 0.5) * 0.3;
      this.size = Math.random() * 1.5 + 0.5;
      this.alpha = Math.random() * 0.3 + 0.1;
      this.life = Math.random() * 200 + 100;
      this.maxLife = this.life;
    }
    update() {
      this.x += this.vx;
      this.y += this.vy;
      this.life--;
      if (this.life <= 0 || this.x < 0 || this.x > w || this.y < 0 || this.y > h) {
        this.reset();
      }
    }
    draw() {
      const p = this.life / this.maxLife;
      ctx.beginPath();
      ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
      ctx.fillStyle = `rgba(0, 102, 204, ${this.alpha * p})`;
      ctx.fill();
    }
  }

  for (let i = 0; i < 60; i++) particles.push(new Particle());

  function animate() {
    ctx.clearRect(0, 0, w, h);
    particles.forEach(p => { p.update(); p.draw(); });
    requestAnimationFrame(animate);
  }
  animate();
})();

// =====================
// 导航
// =====================
const navTabs = document.querySelectorAll('.nav-tab');
const sections = document.querySelectorAll('.section');

navTabs.forEach(tab => {
  tab.addEventListener('click', () => {
    const target = tab.dataset.section;
    navTabs.forEach(t => t.classList.remove('active'));
    tab.classList.add('active');
    sections.forEach(s => s.classList.toggle('active', s.id === target));
  });
});

// =====================
// 状态卡片定义
// =====================
const statusDefs = [
  { id: 'vbs', titleZh: 'VBS', titleEn: 'VBS', descZh: '基于虚拟化的安全', descEn: 'Virtualization-based Security' },
  { id: 'hvci', titleZh: '内存完整性', titleEn: 'Memory Integrity', descZh: 'HVCI / 强制代码完整性', descEn: 'HVCI / Code Integrity' },
  { id: 'cg', titleZh: 'Credential Guard', titleEn: 'Credential Guard', descZh: '凭证隔离保护', descEn: 'Isolated credential storage' },
  { id: 'hyperv', titleZh: 'Hyper-V', titleEn: 'Hyper-V', descZh: 'Hyper-V 虚拟化平台', descEn: 'Hyper-V platform' },
  { id: 'vt', titleZh: 'CPU 虚拟化', titleEn: 'CPU Virtualization', descZh: 'VT-x / AMD-V 硬件支持', descEn: 'VT-x / AMD-V support' },
  { id: 'slat', titleZh: 'SLAT', titleEn: 'SLAT', descZh: '二级地址转换', descEn: 'Second Level Address Translation' },
  { id: 'sb', titleZh: 'Secure Boot', titleEn: 'Secure Boot', descZh: 'UEFI 安全启动', descEn: 'UEFI Secure Boot' },
  { id: 'tpm', titleZh: 'TPM', titleEn: 'TPM', descZh: '可信平台模块', descEn: 'Trusted Platform Module' },
  { id: 'hvlaunch', titleZh: 'Hypervisor 启动', titleEn: 'HV Launch', descZh: 'BCD 虚拟机监控程序启动类型', descEn: 'BCD hypervisorlaunchtype' },
  { id: 'efiguard', titleZh: 'EfiGuard 启动项', titleEn: 'EfiGuard Entry', descZh: 'EfiGuard 引导项状态', descEn: 'EfiGuard bootloader entry' },
];

let lastStatus = null;

function getStatusLabel(value, type) {
  if (value === null || value === undefined) return { text: t('status_unknown'), cls: 'unknown' };

  if (type === 'bool') {
    return value === true || value === 'True' || value === 1
      ? { text: t('status_on'), cls: 'on' }
      : { text: t('status_off'), cls: 'off' };
  }

  if (type === 'vbs') {
    const v = parseInt(value);
    if (v === 0) return { text: t('status_off'), cls: 'off' };
    if (v === 1 || v === 2) return { text: t('status_on'), cls: 'on' };
    return { text: t('status_unknown'), cls: 'unknown' };
  }

  if (type === 'hvci') {
    if (value === 1 || value === true) return { text: t('status_on'), cls: 'on' };
    if (value === 0 || value === false) return { text: t('status_off'), cls: 'off' };
    return { text: t('status_unknown'), cls: 'unknown' };
  }

  if (type === 'credguard') {
    const v = parseInt(value);
    if (v === 0) return { text: t('status_off'), cls: 'off' };
    if (v === 1) return { text: t('status_on'), cls: 'on' };
    if (v === 2) return { text: t('status_warn'), cls: 'warn' };
    return { text: t('status_unknown'), cls: 'unknown' };
  }

  if (type === 'hyperv') {
    const s = String(value).toLowerCase();
    if (s === 'enabled') return { text: t('status_on'), cls: 'on' };
    if (s === 'disabled') return { text: t('status_off'), cls: 'off' };
    if (s.includes('pending')) return { text: t('status_pending'), cls: 'warn' };
    return { text: String(value), cls: 'unknown' };
  }

  if (type === 'hvlaunch') {
    const s = String(value).toLowerCase();
    if (s === 'auto') return { text: 'Auto', cls: 'on' };
    if (s === 'off') return { text: 'Off', cls: 'off' };
    return { text: String(value) || t('status_unknown'), cls: 'unknown' };
  }

  if (type === 'tpm') {
    if (!value || value.Present === undefined) return { text: t('status_unknown'), cls: 'unknown' };
    if (value.Present && value.Enabled) return { text: t('status_on'), cls: 'on' };
    if (value.Present && !value.Enabled) return { text: t('status_warn'), cls: 'warn' };
    if (!value.Present) return { text: t('status_off'), cls: 'off' };
    return { text: t('status_unknown'), cls: 'unknown' };
  }

  if (type === 'efiguard') {
    return value === true
      ? { text: t('status_on'), cls: 'on' }
      : { text: t('status_off'), cls: 'off' };
  }

  return { text: t('status_unknown'), cls: 'unknown' };
}

function renderStatusCards(status) {
  if (!status) return;
  const grid = document.getElementById('status-grid');

  const values = {
    vbs: getStatusLabel(status.vbs, 'vbs'),
    hvci: getStatusLabel(status.hvci, 'hvci'),
    cg: getStatusLabel(status.credentialGuard, 'credguard'),
    hyperv: getStatusLabel(status.hyperV, 'hyperv'),
    vt: getStatusLabel(status.virtualization, 'bool'),
    slat: getStatusLabel(status.slat, 'bool'),
    sb: getStatusLabel(status.secureBoot, 'bool'),
    tpm: getStatusLabel(status.tpm, 'tpm'),
    hvlaunch: getStatusLabel(status.hypervisorLaunchType, 'hvlaunch'),
    efiguard: getStatusLabel(status.efiGuard, 'efiguard'),
  };

  grid.innerHTML = '';
  statusDefs.forEach((def, i) => {
    const val = values[def.id];
    const card = document.createElement('div');
    card.className = `status-card ${val.cls}`;
    card.style.transitionDelay = `${i * 40}ms`;
    card.innerHTML = `
      <div class="status-card-header">
        <span class="status-card-title">${currentLang === 'zh' ? def.titleZh : def.titleEn}</span>
        <span class="status-chip ${val.cls}">${val.text}</span>
      </div>
      <p class="status-card-desc">${currentLang === 'zh' ? def.descZh : def.descEn}</p>
    `;
    grid.appendChild(card);
    requestAnimationFrame(() => card.classList.add('visible'));
  });
}

// =====================
// 主刷新逻辑
// =====================
async function refreshStatus() {
  const btn = document.getElementById('refreshBtn');
  btn.style.animation = 'spin 0.8s linear';
  setTimeout(() => btn.style.animation = '', 800);

  try {
    const s = await window.electronAPI.getSystemStatus();
    lastStatus = s;
    renderStatusCards(s);

    const osText = s.osInfo && (s.osInfo.OsName || s.osInfo.OsVersion)
      ? `${s.osInfo.OsName || ''} ${s.osInfo.OsVersion || ''}`.trim()
      : 'Windows';
    document.getElementById('os-info-text').textContent = osText;

    const date = new Date(s.timestamp || Date.now());
    document.getElementById('last-updated').textContent = date.toLocaleTimeString(currentLang === 'zh' ? 'zh-CN' : 'en-US');

    // HVCI 警告
    const warnPanel = document.getElementById('warning-panel');
    if (s.hvci === 1 || s.hvci === true) {
      warnPanel.style.display = 'flex';
    } else {
      warnPanel.style.display = 'none';
    }
  } catch (err) {
    console.error('Refresh failed:', err);
  }
}

document.getElementById('refreshBtn').addEventListener('click', refreshStatus);

// =====================
// 管理员检测
// =====================
async function checkAdmin() {
  try {
    const admin = await window.electronAPI.isAdmin();
    const banner = document.getElementById('admin-banner');
    const main = document.querySelector('.main-content');
    if (!admin) {
      banner.style.display = 'block';
      main.style.paddingTop = '88px';
      document.getElementById('btnDisableVbs').disabled = true;
      document.getElementById('btnInstallEfi').disabled = true;
    } else {
      banner.style.display = 'none';
      main.style.paddingTop = '44px';
    }
  } catch (e) {
    console.error('Admin check failed:', e);
  }
}

// =====================
// 日志系统
// =====================
function appendLogs(lines) {
  const out = document.getElementById('logs-output');
  const timestamp = new Date().toLocaleTimeString(currentLang === 'zh' ? 'zh-CN' : 'en-US');

  let html = out.querySelector('.logs-placeholder') ? '' : out.innerHTML;
  if (!html && !out.querySelector('.logs-placeholder')) html = out.innerHTML;
  if (out.querySelector('.logs-placeholder')) html = '';

  const block = lines.map(l => {
    let cls = 'log-info';
    if (l.startsWith('[OK]')) cls = 'log-ok';
    else if (l.startsWith('[ERR]')) cls = 'log-err';
    else if (l.startsWith('[WARN]')) cls = 'log-warn';
    else if (l.startsWith('[!]')) cls = 'log-warn';
    return `<span class="${cls}">[${timestamp}] ${escapeHtml(l)}</span>`;
  }).join('\n');

  out.innerHTML = html + (html ? '\n' : '') + block;
  out.scrollTop = out.scrollHeight;

  // 自动切换到日志页
  navTabs.forEach(t => t.classList.remove('active'));
  document.querySelector('[data-section="logs"]').classList.add('active');
  sections.forEach(s => s.classList.toggle('active', s.id === 'logs'));
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

document.getElementById('clearLogsBtn').addEventListener('click', () => {
  document.getElementById('logs-output').innerHTML = `<span class="logs-placeholder">${t('logs_placeholder')}</span>`;
});

// =====================
// 关闭 VBS
// =====================
document.getElementById('btnDisableVbs').addEventListener('click', async () => {
  const confirmed = await window.electronAPI.showConfirm({
    title: t('confirm_disable_vbs_title'),
    message: t('confirm_disable_vbs_msg'),
    detail: t('confirm_disable_vbs_detail'),
    okText: currentLang === 'zh' ? '确认' : 'Confirm',
    cancelText: currentLang === 'zh' ? '取消' : 'Cancel'
  });
  if (!confirmed) return;

  const btn = document.getElementById('btnDisableVbs');
  btn.disabled = true;
  const originalText = btn.innerHTML;
  btn.innerHTML = `<span>${currentLang === 'zh' ? '执行中...' : 'Processing...'}</span>`;

  try {
    const logs = await window.electronAPI.disableVbs();
    appendLogs(logs);
    await refreshStatus();
    await window.electronAPI.showMessage({
      type: 'info',
      title: t('toast_vbs_done'),
      message: t('toast_vbs_done'),
      detail: t('toast_vbs_reboot')
    });
  } catch (err) {
    appendLogs([`[ERR] ${err.message || err}`]);
  } finally {
    btn.disabled = false;
    btn.innerHTML = originalText;
  }
});

// =====================
// EfiGuard 安装
// =====================
async function checkBundledEfiGuard() {
  try {
    const info = await window.electronAPI.getEfiGuardBundled();
    const statusEl = document.getElementById('efi-status');
    if (info.available) {
      statusEl.textContent = t('efi_bundled_ok');
      statusEl.style.color = 'var(--success)';
    } else {
      statusEl.textContent = t('efi_bundled_missing');
      statusEl.style.color = 'var(--danger)';
      document.getElementById('btnInstallEfi').disabled = true;
    }
  } catch (e) {
    console.error(e);
  }
}

document.getElementById('btnInstallEfi').addEventListener('click', async () => {
  const confirmed = await window.electronAPI.showConfirm({
    title: t('confirm_install_efi_title'),
    message: t('confirm_install_efi_msg'),
    detail: t('confirm_install_efi_detail'),
    okText: currentLang === 'zh' ? '确认' : 'Confirm',
    cancelText: currentLang === 'zh' ? '取消' : 'Cancel'
  });
  if (!confirmed) return;

  const btn = document.getElementById('btnInstallEfi');
  btn.disabled = true;
  const originalText = btn.innerHTML;
  btn.innerHTML = `<span>${currentLang === 'zh' ? '安装中...' : 'Installing...'}</span>`;

  try {
    const result = await window.electronAPI.installEfiGuardBundled();
    appendLogs(result.logs);
    if (result.success) {
      await window.electronAPI.showMessage({
        type: 'info',
        title: t('toast_efi_done'),
        message: t('toast_efi_done'),
        detail: t('toast_efi_reboot')
      });
    } else {
      await window.electronAPI.showMessage({
        type: 'error',
        title: 'Error',
        message: result.error || 'Unknown error'
      });
    }
    await refreshStatus();
  } catch (err) {
    appendLogs([`[ERR] ${err.message || err}`]);
  } finally {
    btn.disabled = false;
    btn.innerHTML = originalText;
  }
});

// =====================
// 全局动画关键帧注入
// =====================
const styleSheet = document.createElement('style');
styleSheet.textContent = `
  @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
`;
document.head.appendChild(styleSheet);

// =====================
// 初始化
// =====================
async function init() {
  updateLanguage();
  await checkAdmin();
  await checkBundledEfiGuard();
  await refreshStatus();
  setInterval(refreshStatus, 5000);
}

init();
