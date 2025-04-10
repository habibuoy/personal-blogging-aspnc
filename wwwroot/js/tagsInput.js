const list = document.querySelector("#tags-list");
const hiddenInput = document.querySelector("#tags-hidden");
const input = document.querySelector("#tags-input");
const inputGuide = document.querySelector("#tags-input-guide");

const maxTags = 5;

let tags = hiddenInput.value ? hiddenInput.value.split(', ') : [];

updateCount();
updateTags();

function updateCount() {
    const remainingCount = maxTags - tags.length;
    const maxReached = remainingCount <= 0;
    inputGuide.innerText = maxReached ? `Maximum Tags` : `Press enter or comma after each tag (${remainingCount} tag${remainingCount > 1 ? "s" : ""} remaining).`;
    input.disabled = maxReached;
}

function updateTags() {
    list.querySelectorAll("#tags-item").forEach(li => li.remove());
    tags.slice().reverse().forEach(tag => {
        let liTag = `<li id="tags-item">${tag} <i class="bi bi-x" onclick="remove(this, '${tag}')"></i></li>`;
        list.insertAdjacentHTML("afterbegin", liTag);
    });
    updateCount();
}

function remove(element, tag) {
    let index  = tags.indexOf(tag);
    tags = [...tags.slice(0, index), ...tags.slice(index + 1)];
    element.parentElement.remove();
    updateHiddenInput();
    updateCount();
}

function updateHiddenInput() {
    hiddenInput.value = tags.join(', ');
}

function addTag(event){

    if(['Enter', ','].includes(event.key)) {

        if (document.activeElement.id === "tags-input")
        {
            let tag = event.target.value.replace(/\s+/g, ' ');
            if (tag.length <= 1
                || tags.includes(tag)
                || tags.length >= maxTags)
            {
                return;
            }
            
            tag.split(',').forEach(tag => {
                if (tag === "") return;
                tags.push(tag);
                updateTags();
            });
            
            updateHiddenInput();
            
            event.target.value = "";
        }
    }
}

input.addEventListener("keyup", addTag);
input.addEventListener("keydown", function(event) {
    if(['Enter', ','].includes(event.key)){

        if (document.activeElement.id === "tags-input")
        {
            // prevent submitting the form
            event.preventDefault();
        }
    }
});

const removeAllTagsBtn = document.querySelector("#tags-input-details button");

removeAllTagsBtn.addEventListener("click", () =>{
    tags.length = 0;
    list.querySelectorAll("#tags-item").forEach(li => li.remove());
    updateCount();
});