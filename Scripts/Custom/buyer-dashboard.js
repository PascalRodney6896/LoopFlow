// ============================================
// LOOP FLOW - Buyer Dashboard Interactions
// ============================================

document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    // ---------- TAB NAVIGATION ----------
    const navItems = document.querySelectorAll('.nav-item');
    const tabContents = {
        home: document.getElementById('home-tab'),
        orders: document.getElementById('orders-tab'),
        credit: document.getElementById('credit-tab'),
        forecast: document.getElementById('forecast-tab'),
        profile: document.getElementById('profile-tab')
    };

    function navigateToTab(tabName) {
        // Update nav items
        navItems.forEach(item => {
            item.classList.remove('active');
            if (item.dataset.tab === tabName) {
                item.classList.add('active');
            }
        });

        // Update tab content
        Object.keys(tabContents).forEach(key => {
            if (tabContents[key]) {
                tabContents[key].classList.remove('active');
            }
        });

        if (tabContents[tabName]) {
            tabContents[tabName].classList.add('active');
        }

        // Close slide menu if open
        closeSlideMenu();
    }

    // Make navigateToTab globally available for onclick handlers
    window.navigateToTab = navigateToTab;

    navItems.forEach(item => {
        item.addEventListener('click', function (e) {
            const tab = this.dataset.tab;
            if (tab) {
                navigateToTab(tab);
            }
        });
    });

    // ---------- SLIDE MENU (Hamburger) ----------
    const hamburgerBtn = document.getElementById('hamburgerMenu');
    const closeMenuBtn = document.getElementById('closeMenu');
    const slideMenu = document.getElementById('slideMenu');
    const slideOverlay = document.getElementById('slideOverlay');

    function openSlideMenu() {
        slideMenu.classList.add('open');
        slideOverlay.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    function closeSlideMenu() {
        slideMenu.classList.remove('open');
        slideOverlay.classList.remove('active');
        document.body.style.overflow = '';
    }

    if (hamburgerBtn) {
        hamburgerBtn.addEventListener('click', openSlideMenu);
    }

    if (closeMenuBtn) {
        closeMenuBtn.addEventListener('click', closeSlideMenu);
    }

    if (slideOverlay) {
        slideOverlay.addEventListener('click', closeSlideMenu);
    }

    // Close menu on escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeSlideMenu();
        }
    });

    // Close menu on menu item click
    document.querySelectorAll('.menu-item').forEach(item => {
        item.addEventListener('click', function (e) {
            // Don't close if it's the logout button (it has its own action)
            if (!this.classList.contains('text-danger')) {
                closeSlideMenu();
            }
        });
    });

    // ---------- SWEEP SIMULATOR ----------
    const sweepTriggerBtn = document.querySelector('.sweep-trigger-btn');
    const sweepInput = document.querySelector('.sweep-input');

    if (sweepTriggerBtn && sweepInput) {
        sweepTriggerBtn.addEventListener('click', function () {
            const amount = parseFloat(sweepInput.value) || 0;
            if (amount > 0) {
                const sweepAmount = amount * 0.30; // 30% sweep
                // Show feedback
                const originalText = this.innerHTML;
                this.innerHTML = `
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M5 13l4 4L19 7"/>
                    </svg>
                    Swept KES ${sweepAmount.toLocaleString()}
                `;
                this.style.background = '#10b981';

                setTimeout(() => {
                    this.innerHTML = originalText;
                    this.style.background = '';
                }, 3000);
            } else {
                // Shake animation for invalid input
                sweepInput.style.borderColor = '#ef4444';
                setTimeout(() => {
                    sweepInput.style.borderColor = '';
                }, 1000);
            }
        });

        // Trigger on Enter key
        sweepInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                sweepTriggerBtn.click();
            }
        });
    }

    // ---------- CREDIT ACTION BUTTON ----------
    const creditActionBtn = document.querySelector('.credit-action-btn');
    if (creditActionBtn) {
        creditActionBtn.addEventListener('click', function () {
            // Navigate to forecast tab or show financing modal
            navigateToTab('forecast');
        });
    }

    // ---------- NEW ORDER BUTTON ----------
    const newOrderBtn = document.querySelector('.new-order-btn');
    if (newOrderBtn) {
        newOrderBtn.addEventListener('click', function () {
            // Navigate to order creation or show modal
            // For now, just show a toast/alert
            showToast('Create New Order', 'Order creation form will open here', 'info');
        });
    }

    // ---------- FORECAST ACTION BUTTON ----------
    const forecastActionBtn = document.querySelector('.forecast-action-btn');
    if (forecastActionBtn) {
        forecastActionBtn.addEventListener('click', function () {
            // Navigate to financing request
            showToast('Financing Request', 'Financing KES 12,500 for inventory gap', 'success');
        });
    }

    // ---------- TOAST NOTIFICATION SYSTEM ----------
    function showToast(title, message, type = 'info') {
        // Remove existing toast
        const existingToast = document.querySelector('.toast-notification');
        if (existingToast) {
            existingToast.remove();
        }

        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;

        const icons = {
            success: '✓',
            error: '✕',
            warning: '',
            info: ''
        };

        toast.innerHTML = `
            <div class="toast-icon">${icons[type] || ''}</div>
            <div class="toast-content">
                <div class="toast-title">${title}</div>
                <div class="toast-message">${message}</div>
            </div>
            <button class="toast-close">×</button>
            <div class="toast-progress"></div>
        `;

        document.body.appendChild(toast);

        // Show toast
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);

        // Auto-dismiss after 4 seconds
        const timeout = setTimeout(() => {
            dismissToast(toast);
        }, 4000);

        // Close button
        const closeBtn = toast.querySelector('.toast-close');
        closeBtn.addEventListener('click', () => {
            clearTimeout(timeout);
            dismissToast(toast);
        });

        // Pause on hover
        toast.addEventListener('mouseenter', () => {
            clearTimeout(timeout);
        });

        toast.addEventListener('mouseleave', () => {
            const newTimeout = setTimeout(() => {
                dismissToast(toast);
            }, 2000);
            toast.dataset.timeout = newTimeout;
        });
    }

    function dismissToast(toast) {
        toast.classList.remove('show');
        setTimeout(() => {
            toast.remove();
        }, 300);
    }

    // Make showToast globally available
    window.showToast = showToast;

    // ---------- ACTIVE TAB FROM URL HASH ----------
    // Check if URL has hash and navigate to that tab
    if (window.location.hash) {
        const hash = window.location.hash.replace('#', '');
        if (['home', 'orders', 'forecast', 'profile'].includes(hash)) {
            navigateToTab(hash);
        }
    }

    // Update URL hash when tab changes
    const originalNavigate = navigateToTab;
    window.navigateToTab = function (tabName) {
        originalNavigate(tabName);
        if (tabName && ['home', 'orders', 'forecast', 'profile'].includes(tabName)) {
            window.location.hash = tabName;
        }
    };


    // ---------- BALANCE CARD TAP TO TOGGLE ----------
    const balanceCard = document.getElementById('balanceCard');
    const balanceLabel = document.getElementById('balanceLabel');
    const balanceAmount = document.getElementById('balanceAmount');
    let showingLoopWallet = false;

    // Store the original values
    const originalLabel = 'Total Balance';
    const originalAmount = balanceAmount.textContent;
    const loopWalletLabel = 'LOOP Wallet Balance';
    const loopWalletAmount = '142500'; // Use actual wallet balance

    if (balanceCard) {
        balanceCard.addEventListener('click', function (e) {
            // Don't trigger if clicking on a button inside the card
            if (e.target.closest('.action-btn')) return;

            showingLoopWallet = !showingLoopWallet;

            if (showingLoopWallet) {
                balanceLabel.textContent = loopWalletLabel;
                balanceAmount.textContent = loopWalletAmount;
                // Update the tap hint
                const hint = this.querySelector('.tap-hint span');
                if (hint) hint.textContent = 'Tap to view LOOP Account';
            } else {
                balanceLabel.textContent = originalLabel;
                balanceAmount.textContent = originalAmount;
                const hint = this.querySelector('.tap-hint span');
                if (hint) hint.textContent = 'Tap to view LOOP Wallet';
            }
        });
    }

    // ---------- SUB TABS (Orders/Transactions) ----------
    const subTabs = document.querySelectorAll('.sub-tab');
    const subTabContents = {
        'orders-sub': document.getElementById('orders-sub'),
        'transactions-sub': document.getElementById('transactions-sub')
    };

    subTabs.forEach(tab => {
        tab.addEventListener('click', function () {
            const target = this.dataset.subtab;

            // Update sub-tab buttons
            subTabs.forEach(t => t.classList.remove('active'));
            this.classList.add('active');

            // Update sub-tab content
            Object.keys(subTabContents).forEach(key => {
                if (subTabContents[key]) {
                    subTabContents[key].classList.remove('active');
                }
            });

            if (subTabContents[target]) {
                subTabContents[target].classList.add('active');
            }
        });
    });

    // ---------- ORDER FILTERS ----------
    const filterBtns = document.querySelectorAll('.orders-filters .filter-btn');
    const orderRows = document.querySelectorAll('.orders-table tbody tr');

    filterBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            // Update active state
            filterBtns.forEach(b => b.classList.remove('active'));
            this.classList.add('active');

            const filter = this.dataset.filter;

            orderRows.forEach(row => {
                const status = row.querySelector('.status-badge');
                if (status) {
                    const statusText = status.textContent.toLowerCase();
                    if (filter === 'all' || statusText === filter) {
                        row.style.display = '';
                    } else {
                        row.style.display = 'none';
                    }
                }
            });
        });
    });

    // ---------- TRANSACTION FILTERS ----------
    const transFilterBtns = document.querySelectorAll('.transactions-filters .filter-btn');
    const transactionItems = document.querySelectorAll('.transaction-item');

    transFilterBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            // Update active state
            transFilterBtns.forEach(b => b.classList.remove('active'));
            this.classList.add('active');

            const filter = this.dataset.filter;

            transactionItems.forEach(item => {
                const title = item.querySelector('.transaction-title');
                if (title) {
                    const titleText = title.textContent.toLowerCase();
                    let show = false;

                    if (filter === 'all') {
                        show = true;
                    } else if (filter === 'topup' && titleText.includes('top-up')) {
                        show = true;
                    } else if (filter === 'transfer' && titleText.includes('transfer')) {
                        show = true;
                    } else if (filter === 'orders' && titleText.includes('order')) {
                        show = true;
                    }

                    item.style.display = show ? '' : 'none';
                }
            });
        });
    });

    // ---------- NAVIGATE TO TAB WITH SUBTAB ----------
    // Override the navigateToTab function to handle orders tab with default sub-tab
    const originalNavigateToTab = window.navigateToTab;
    window.navigateToTab = function (tabName) {
        originalNavigateToTab(tabName);

        // If navigating to orders tab, ensure orders sub-tab is active
        if (tabName === 'orders') {
            const ordersSubTab = document.querySelector('.sub-tab[data-subtab="orders-sub"]');
            if (ordersSubTab) {
                ordersSubTab.click();
            }
        }
    };


    console.log(' LOOP FLOW Buyer Dashboard initialized');
});