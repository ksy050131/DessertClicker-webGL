document.addEventListener('DOMContentLoaded', () => {

    /* =========================================
       1. Widget Drag & Drop and Z-Index Logic
       ========================================= */
    const widgets = document.querySelectorAll('.widget');
    let highestZIndex = 150; // Start higher than LeftNav (90)

    function bringToFront(widget) {
        highestZIndex++;
        widget.style.zIndex = highestZIndex;
    }

    widgets.forEach(widget => {
        const header = widget.querySelector('.widget-header');

        let isDragging = false;
        let startX, startY;
        let initialLeft, initialTop;

        widget.addEventListener('mousedown', () => bringToFront(widget));
        widget.addEventListener('touchstart', () => bringToFront(widget), { passive: true });

        // Helper to get clientX / clientY from either Mouse or Touch event
        function getEventPoint(e) {
            return e.touches ? e.touches[0] : e;
        }

        function dragStart(e) {
            if (e.target.closest('.close-btn') || e.target.closest('.add-todo-btn')) return;

            const pt = getEventPoint(e);
            isDragging = true;
            startX = pt.clientX;
            startY = pt.clientY;

            const style = window.getComputedStyle(widget);
            initialLeft = parseInt(style.left, 10) || 0;
            initialTop = parseInt(style.top, 10) || 0;
            document.body.style.userSelect = 'none';
        }

        function dragMove(e) {
            if (!isDragging) return;
            // Prevent scrolling while dragging a widget on mobile
            if (e.type === 'touchmove') e.preventDefault();

            const pt = getEventPoint(e);
            widget.style.left = `${initialLeft + (pt.clientX - startX)}px`;
            widget.style.top = `${initialTop + (pt.clientY - startY)}px`;
        }

        function dragEnd() {
            if (isDragging) {
                isDragging = false;
                document.body.style.userSelect = '';
            }
        }

        // Mouse Events
        header.addEventListener('mousedown', dragStart);
        document.addEventListener('mousemove', dragMove);
        document.addEventListener('mouseup', dragEnd);

        // Touch Events
        header.addEventListener('touchstart', dragStart, { passive: false });
        document.addEventListener('touchmove', dragMove, { passive: false });
        document.addEventListener('touchend', dragEnd);

        const closeBtn = widget.querySelector('.close-btn');
        if (closeBtn) {
            closeBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                widget.style.display = 'none';
            });
        }
    });

    /* =========================================
       2. Left Navigation (Widget Toggles)
       ========================================= */
    const navBtns = document.querySelectorAll('.nav-btn');
    navBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const targetId = btn.getAttribute('data-widget-target');
            const targetWidget = document.getElementById(targetId);
            if (!targetWidget) return;

            // If hidden or effectively hidden, show it
            if (window.getComputedStyle(targetWidget).display === 'none') {
                targetWidget.style.display = 'flex';
                bringToFront(targetWidget);
            } else {
                // If it is visible, could be in the back.
                const currentZ = parseInt(window.getComputedStyle(targetWidget).zIndex, 10) || 0;
                if (currentZ < highestZIndex) {
                    bringToFront(targetWidget);
                } else {
                    targetWidget.style.display = 'none'; // toggle off if already on top
                }
            }
        });
    });

    /* =========================================
       3. Todolist Logic
       ========================================= */
    const todolistWidget = document.getElementById('Todolist');
    const addTodoBtn = todolistWidget.querySelector('.add-todo-btn');
    const addTodoForm = todolistWidget.querySelector('.add-todo-form');
    const newTodoInput = document.getElementById('new-todo-input');
    const todoListContainer = document.getElementById('todo-list-container');
    let todoCounter = 2; // Starting after todo1

    addTodoBtn.addEventListener('click', () => {
        addTodoForm.style.display = addTodoForm.style.display === 'none' ? 'block' : 'none';
        if (addTodoForm.style.display === 'block') {
            newTodoInput.focus();
        }
    });

    newTodoInput.addEventListener('keydown', (e) => {
        if (e.isComposing || e.keyCode === 229) return;
        if (e.key === 'Enter' && newTodoInput.value.trim() !== '') {
            todoCounter++;
            const newId = `todo${todoCounter}`;
            const text = newTodoInput.value.trim();

            const newDiv = document.createElement('div');
            newDiv.className = 'todo-item';
            newDiv.innerHTML = `
                <input type="checkbox" id="${newId}">
                <span class="todo-label">${text}</span>
                <span class="trash-btn">🗑</span>
            `;
            todoListContainer.appendChild(newDiv);

            newTodoInput.value = '';
            addTodoForm.style.display = 'none';
        }
    });

    // Delegation for Trash and Inline Edit
    todoListContainer.addEventListener('click', (e) => {
        // Trash
        if (e.target.classList.contains('trash-btn')) {
            const item = e.target.closest('.todo-item');
            if (item) item.remove();
        }

        // Inline edit
        if (e.target.classList.contains('todo-label')) {
            const labelSpan = e.target;
            const currentText = labelSpan.textContent;

            const input = document.createElement('input');
            input.type = 'text';
            input.className = 'todo-edit-input';
            input.value = currentText;

            labelSpan.replaceWith(input);
            input.focus();

            const saveEdit = () => {
                const newText = input.value.trim() || currentText; // Fallback if empty
                labelSpan.textContent = newText;
                input.replaceWith(labelSpan);
            };

            input.addEventListener('blur', saveEdit);
            input.addEventListener('keydown', (ev) => {
                if (ev.isComposing || ev.keyCode === 229) return;
                if (ev.key === 'Enter') saveEdit();
            });
        }
    });

    // Checkbox completion logic
    todoListContainer.addEventListener('change', (e) => {
        if (e.target.type === 'checkbox' && e.target.checked) {
            if (typeof unityInstance !== 'undefined' && unityInstance !== null) {
                unityInstance.SendMessage("Canvas", "AddCoinFromWeb", 50);
                console.log("[Web->Unity] 투두리스트 완료! 50 코인 전송");
            } else {
                console.log("[Web Only] 투두리스트 완료 (코인 50 획득 대기) - 유니티 인스턴스 미연결");
            }
        }
    });

    /* =========================================
       4. Fomodoro Timer Logic
       ========================================= */
    const fomoWidget = document.getElementById('Fomodoro');
    const timeDisplay = fomoWidget.querySelector('.time-display');
    const timeSelect = fomoWidget.querySelector('.time-select');
    const startBtn = fomoWidget.querySelector('.StartButton');
    const stopBtn = fomoWidget.querySelector('.StopButton');

    let initialMinutes = 25; // Store the chosen minutes
    let totalSeconds = initialMinutes * 60;
    let timerInterval = null;
    let isRunning = false;

    function updateDisplay(seconds) {
        const m = Math.floor(seconds / 60).toString().padStart(2, '0');
        const s = (seconds % 60).toString().padStart(2, '0');
        timeDisplay.textContent = `${m}:${s}`;
    }

    // Edit time logic
    timeDisplay.addEventListener('click', () => {
        if (!isRunning) {
            // Instead of hiding timeDisplay (which causes layout jump), 
            // we'll just show the select overlay on top of it.
            timeDisplay.style.opacity = '0'; // Hide text visually but keep DOM space
            timeSelect.style.display = 'inline-block';
            timeSelect.focus();
        }
    });

    timeSelect.addEventListener('change', (e) => {
        initialMinutes = parseInt(e.target.value, 10);
        totalSeconds = initialMinutes * 60;
        updateDisplay(totalSeconds);
        // Hide select, restore display visibility
        timeSelect.style.display = 'none';
        timeDisplay.style.opacity = '1';
    });

    timeSelect.addEventListener('blur', () => {
        timeSelect.style.display = 'none';
        timeDisplay.style.opacity = '1';
    });

    // Start / Stop logic
    startBtn.addEventListener('click', () => {
        if (isRunning || totalSeconds <= 0) return;
        isRunning = true;

        timeSelect.style.display = 'none';
        timeDisplay.style.display = 'inline-block';

        timerInterval = setInterval(() => {
            totalSeconds--;
            updateDisplay(totalSeconds);
            if (totalSeconds <= 0) {
                clearInterval(timerInterval);
                isRunning = false;

                // Unity Reward Logic
                const rewardCoins = initialMinutes * 20;
                if (typeof unityInstance !== 'undefined' && unityInstance !== null) {
                    unityInstance.SendMessage("Canvas", "AddCoinFromWeb", rewardCoins);
                    console.log(`[Web->Unity] ${initialMinutes}분 집중 완료! ${rewardCoins} 코인 전송`);
                } else {
                    console.log(`[Web Only] ${initialMinutes}분 집중 완료! (코인 ${rewardCoins} 획득 대기) - 유니티 인스턴스 미연결`);
                }
            }
        }, 1000);
    });

    stopBtn.addEventListener('click', () => {
        if (!isRunning) return;
        clearInterval(timerInterval);
        isRunning = false;
    });

    /* =========================================
       5. Music Player Logic
       ========================================= */
    const urlInput = document.getElementById('music-url-input');
    const urlSubmit = document.getElementById('music-url-submit');
    const embedContainer = document.getElementById('MusicEmbedContainer');

    function getEmbedIframe(url) {
        let embedUrl = '';

        // YouTube short or long format
        if (url.includes('youtube.com/watch') || url.includes('youtu.be/')) {
            let videoId = '';
            if (url.includes('youtube.com/watch')) {
                const params = new URLSearchParams(url.split('?')[1]);
                videoId = params.get('v');
            } else if (url.includes('youtu.be/')) {
                videoId = url.split('youtu.be/')[1].split('?')[0];
            }
            if (videoId) {
                // Use standard youtube embed without origin (since file:// origins fail strict checks)
                // Added loop=1&playlist=videoId to make it auto-repeat
                embedUrl = `https://www.youtube.com/embed/${videoId}?rel=0&loop=1&playlist=${videoId}`;
            }
        }
        // Spotify formats
        else if (url.includes('spotify.com/')) {
            const match = url.match(/spotify\.com\/(track|album|playlist|episode)\/([a-zA-Z0-9]+)/);
            if (match) {
                const type = match[1];
                const id = match[2];
                embedUrl = `https://open.spotify.com/embed/${type}/${id}`;
            }
        }

        if (embedUrl) {
            return `<iframe src="${embedUrl}" title="Music Player Embed" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen loading="lazy"></iframe>`;
        }
        return `<div class="music-placeholder">Invalid or unsupported URL format</div>`;
    }

    urlSubmit.addEventListener('click', () => {
        const url = urlInput.value.trim();
        if (!url) return;
        embedContainer.innerHTML = getEmbedIframe(url);
    });

    urlInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            urlSubmit.click();
        }
    });

});
