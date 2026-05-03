<script>
  import { createEventDispatcher } from 'svelte';
  export let schema;
  const dispatch = createEventDispatcher();

  let currentSchema = schema ? JSON.parse(JSON.stringify(schema)) : { id: '', title: '', description: '', steps: [] };

  $: if (schema) {
    currentSchema = JSON.parse(JSON.stringify(schema));
  }

  function addStep() {
    currentSchema.steps.push({ title: 'New Step', fields: [] });
    currentSchema = { ...currentSchema };
  }

  function removeStep(index) {
    currentSchema.steps.splice(index, 1);
    currentSchema = { ...currentSchema };
  }

  function addField(stepIndex) {
    currentSchema.steps[stepIndex].fields.push({ name: '', label: '', type: 'text', required: false, options: [] });
    currentSchema = { ...currentSchema };
  }

  function removeField(stepIndex, fieldIndex) {
    currentSchema.steps[stepIndex].fields.splice(fieldIndex, 1);
    currentSchema = { ...currentSchema };
  }

  function save() {
    dispatch('save', currentSchema);
  }
</script>

<div class="rounded-3xl border border-slate-800 bg-slate-950 p-6 shadow-xl">
  <div class="grid gap-4 sm:grid-cols-[1fr_2fr] sm:items-end">
    <div>
      <label for="formTitle" class="block text-sm font-semibold text-white">Form Title</label>
      <input
        id="formTitle"
        class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"
        bind:value={currentSchema.title}
      />
    </div>
    <div>
      <label for="formDescription" class="block text-sm font-semibold text-white">Form Description</label>
      <textarea
        id="formDescription"
        rows="3"
        class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"
        bind:value={currentSchema.description}
      ></textarea>
    </div>
  </div>

  <div class="mt-6">
    {#each currentSchema.steps as step, stepIndex}
      <div class="mb-6 rounded-3xl border border-slate-800 bg-slate-900 p-5">
        <div class="flex items-center justify-between">
          <input
            class="w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white"
            bind:value={step.title}
            placeholder="Step Title"
          />
          <button class="ml-4 rounded-2xl bg-red-500 px-3 py-2 text-sm font-semibold text-white" on:click={() => removeStep(stepIndex)}>Remove Step</button>
        </div>

        <div class="mt-4 space-y-4">
          {#each step.fields as field, fieldIndex}
            <div class="rounded-2xl border border-slate-800 bg-slate-950 p-4">
              <div class="flex items-center justify-between">
                <span class="text-sm font-medium text-white">Field {fieldIndex + 1}</span>
                <button class="rounded-2xl bg-red-500 px-2 py-1 text-xs font-semibold text-white" on:click={() => removeField(stepIndex, fieldIndex)}>Remove</button>
              </div>

              <div class="mt-3 grid gap-3 sm:grid-cols-2">
                <div>
                  <label class="block text-xs font-medium text-slate-400">Name</label>
                  <input
                    class="w-full rounded-2xl border border-slate-800 bg-slate-900 px-3 py-2 text-white text-sm"
                    bind:value={field.name}
                    placeholder="fieldName"
                  />
                </div>

                <div>
                  <label class="block text-xs font-medium text-slate-400">Label</label>
                  <input
                    class="w-full rounded-2xl border border-slate-800 bg-slate-900 px-3 py-2 text-white text-sm"
                    bind:value={field.label}
                    placeholder="Field Label"
                  />
                </div>

                <div>
                  <label class="block text-xs font-medium text-slate-400">Type</label>
                  <select
                    class="w-full rounded-2xl border border-slate-800 bg-slate-900 px-3 py-2 text-white text-sm"
                    bind:value={field.type}
                  >
                    <option value="text">Text</option>
                    <option value="email">Email</option>
                    <option value="number">Number</option>
                    <option value="select">Select</option>
                    <option value="checkbox">Checkbox</option>
                    <option value="date">Date</option>
                  </select>
                </div>

                <div>
                  <label class="block text-xs font-medium text-slate-400">Required</label>
                  <input
                    type="checkbox"
                    class="rounded border-slate-800 bg-slate-900"
                    bind:checked={field.required}
                  />
                </div>
              </div>

              {#if field.type === 'select'}
                <div class="mt-3">
                  <label class="block text-xs font-medium text-slate-400">Options (comma separated)</label>
                  <input
                    class="w-full rounded-2xl border border-slate-800 bg-slate-900 px-3 py-2 text-white text-sm"
                    bind:value={field.options}
                    placeholder="Option1, Option2"
                  />
                </div>
              {/if}
            </div>
          {/each}

          <button class="rounded-2xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white" on:click={() => addField(stepIndex)}>Add Field</button>
        </div>
      </div>
    {/each}

    <button class="rounded-2xl bg-green-500 px-4 py-3 text-sm font-semibold text-white" on:click={addStep}>Add Step</button>
  </div>

  <div class="mt-6 flex justify-end">
    <button class="rounded-2xl bg-sky-500 px-6 py-3 text-sm font-semibold text-white" on:click={save}>Save Form</button>
  </div>
</div>
