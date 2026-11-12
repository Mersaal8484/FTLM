import { describe, expect, it } from 'vitest';
import { appTranslations } from '../i18n';
import { operationalActions } from './OperationsHub';

const read = (language, path) => path.split('.').reduce((value, key) => value?.[key], appTranslations[language].translation);

describe('OperationsHub translations', () => {
  it.each(['en', 'ar'])('provides a translated title and description for every action in %s', language => {
    operationalActions.forEach(({ key }) => {
      ['title', 'description'].forEach(field => {
        const path = `operational.actions.${key}.${field}`;
        const value = read(language, path);
        expect(value, path).toBeTruthy();
        expect(value).not.toBe(path);
      });
    });
  });
});
