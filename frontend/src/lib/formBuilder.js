export function createEmptyField() {
  return {
    name: '',
    label: '',
    type: 'text',
    required: false,
    options: [],
    conditions: [],
    validators: []
  };
}

export function ensureFieldNames(schema) {
  return {
    ...schema,
    fields: schema.fields.map((field, index) => ({
      ...field,
      name: field.name || `field${index + 1}`
    }))
  };
}

export function buildInitialValues(schema) {
  return schema.fields.reduce((map, field) => {
    map[field.name] = field.defaultValue ?? '';
    return map;
  }, {});
}

export function isFieldVisible(field, values) {
  if (!field.conditions || field.conditions.length === 0) return true;

  return field.conditions.every(condition => {
    const raw = values[condition.fieldName];
    const left = raw === undefined || raw === null ? '' : raw.toString();
    const right = condition.value === undefined || condition.value === null ? '' : condition.value.toString();

    switch (condition.operator) {
      case 'equals':
        return left === right;
      case 'not_equals':
        return left !== right;
      case 'greater_than':
        return parseFloat(left) > parseFloat(right);
      case 'less_than':
        return parseFloat(left) < parseFloat(right);
      default:
        return false;
    }
  });
}
